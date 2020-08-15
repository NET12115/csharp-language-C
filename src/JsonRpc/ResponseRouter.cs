using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc.Client;

namespace OmniSharp.Extensions.JsonRpc
{
    public class ResponseRouter : IResponseRouter
    {
        internal readonly IOutputHandler OutputHandler;
        internal readonly ISerializer Serializer;
        private readonly IHandlerTypeDescriptorProvider<IHandlerTypeDescriptor> _handlerTypeDescriptorProvider;

        internal readonly ConcurrentDictionary<long, (string method, TaskCompletionSource<JToken> pendingTask)> Requests =
            new ConcurrentDictionary<long, (string method, TaskCompletionSource<JToken> pendingTask)>();

        public ResponseRouter(IOutputHandler outputHandler, ISerializer serializer, IHandlerTypeDescriptorProvider<IHandlerTypeDescriptor> handlerTypeDescriptorProvider)
        {
            OutputHandler = outputHandler;
            Serializer = serializer;
            _handlerTypeDescriptorProvider = handlerTypeDescriptorProvider;
        }

        public void SendNotification(string method) =>
            OutputHandler.Send(
                new OutgoingNotification {
                    Method = method
                }
            );

        public void SendNotification<T>(string method, T @params) =>
            OutputHandler.Send(
                new OutgoingNotification {
                    Method = method,
                    Params = @params
                }
            );

        public void SendNotification(IRequest @params) => SendNotification(GetMethodName(@params.GetType()), @params);

        public Task<TResponse> SendRequest<TResponse>(IRequest<TResponse> @params, CancellationToken cancellationToken) =>
            SendRequest(GetMethodName(@params.GetType()), @params).Returning<TResponse>(cancellationToken);

        public IResponseRouterReturns SendRequest(string method) => new ResponseRouterReturnsImpl(this, method, new object());

        public IResponseRouterReturns SendRequest<T>(string method, T @params) => new ResponseRouterReturnsImpl(this, method, @params);

        public (string method, TaskCompletionSource<JToken> pendingTask) GetRequest(long id)
        {
            Requests.TryGetValue(id, out var source);
            return source;
        }

        private string GetMethodName(Type type) =>
            _handlerTypeDescriptorProvider.GetMethodName(type) ?? throw new NotSupportedException($"Unable to infer method name for type {type.FullName}");

        private class ResponseRouterReturnsImpl : IResponseRouterReturns
        {
            private readonly ResponseRouter _router;
            private readonly string _method;
            private readonly object _params;

            public ResponseRouterReturnsImpl(ResponseRouter router, string method, object @params)
            {
                _router = router;
                _method = method;
                _params = @params;
            }

            public async Task<TResponse> Returning<TResponse>(CancellationToken cancellationToken)
            {
                var nextId = _router.Serializer.GetNextId();
                var tcs = new TaskCompletionSource<JToken>();
                _router.Requests.TryAdd(nextId, ( _method, tcs ));

                cancellationToken.ThrowIfCancellationRequested();

                _router.OutputHandler.Send(
                    new OutgoingRequest {
                        Method = _method,
                        Params = _params,
                        Id = nextId
                    }
                );
                cancellationToken.Register(
                    () => {
                        if (tcs.Task.IsCompleted) return;
                        _router.CancelRequest(new CancelParams { Id = nextId });
                    }
                );

                try
                {
                    var result = await tcs.Task;
                    if (typeof(TResponse) == typeof(Unit))
                    {
                        return (TResponse) (object) Unit.Value;
                    }

                    return result.ToObject<TResponse>(_router.Serializer.JsonSerializer);
                }
                finally
                {
                    _router.Requests.TryRemove(nextId, out _);
                }
            }

            public async Task ReturningVoid(CancellationToken cancellationToken) => await Returning<Unit>(cancellationToken);
        }
    }
}
