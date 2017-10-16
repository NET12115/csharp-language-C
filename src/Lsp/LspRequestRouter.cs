﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.JsonRpc.Server;
using OmniSharp.Extensions.JsonRpc.Server.Messages;
using OmniSharp.Extensions.LanguageServer.Abstractions;
using OmniSharp.Extensions.LanguageServer.Messages;
using OmniSharp.Extensions.LanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;

namespace OmniSharp.Extensions.LanguageServer
{
    class LspRequestRouter : IRequestRouter
    {
        private readonly IHandlerCollection _collection;
        private ITextDocumentSyncHandler[] _textDocumentSyncHandlers;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _requests = new ConcurrentDictionary<string, CancellationTokenSource>();

        public LspRequestRouter(IHandlerCollection collection)
        {
            _collection = collection;
        }

        private string GetId(object id)
        {
            if (id is string s)
            {
                return s;
            }

            if (id is long l)
            {
                return l.ToString();
            }

            return id?.ToString();
        }

        private ILspHandlerDescriptor FindDescriptor(string method, JToken @params)
        {
            var descriptor = _collection.FirstOrDefault(x => x.Method == method);
            if (descriptor is null) return null;

            if (_textDocumentSyncHandlers == null)
            {
                _textDocumentSyncHandlers = _collection
                    .Select(x => x.Handler is ITextDocumentSyncHandler r ? r : null)
                    .Where(x => x != null)
                    .ToArray();
            }

            if (typeof(ITextDocumentIdentifierParams).GetTypeInfo().IsAssignableFrom(descriptor.Params))
            {
                var textDocumentIdentifierParams = @params.ToObject(descriptor.Params) as ITextDocumentIdentifierParams;
                var attributes = _textDocumentSyncHandlers
                    .Select(x => x.GetTextDocumentAttributes(textDocumentIdentifierParams.TextDocument.Uri))
                    .Where(x => x != null);

                return GetHandler(method, attributes);
            }
            else if (typeof(DidOpenTextDocumentParams).GetTypeInfo().IsAssignableFrom(descriptor.Params))
            {
                var openTextDocumentParams = @params.ToObject(descriptor.Params) as DidOpenTextDocumentParams;
                var attributes = new TextDocumentAttributes(openTextDocumentParams.TextDocument.Uri, openTextDocumentParams.TextDocument.LanguageId);

                return GetHandler(method, attributes);
            }

            // TODO: How to split these
            // Do they fork and join?
            return descriptor;
        }

        private ILspHandlerDescriptor GetHandler(string method, IEnumerable<TextDocumentAttributes> attributes)
        {
            return attributes
                .Select(x => GetHandler(method, x))
                .FirstOrDefault(x => x != null);
        }

        private ILspHandlerDescriptor GetHandler(string method, TextDocumentAttributes attributes)
        {
            foreach (var handler in _collection.Where(x => x.Method == method))
            {
                var registrationOptions = handler.Registration.RegisterOptions as TextDocumentRegistrationOptions;
                if (registrationOptions.DocumentSelector == null || registrationOptions.DocumentSelector.IsMatch(attributes))
                {
                    return handler;
                }
            }
            return null;
        }

        public async void RouteNotification(Notification notification)
        {
            var handler = FindDescriptor(notification.Method, notification.Params);
            if (handler is null) { return; }

            Task result;
            if (handler.Params is null)
            {
                result = ReflectionRequestHandlers.HandleNotification(handler);
            }
            else
            {
                var @params = notification.Params.ToObject(handler.Params);
                result = ReflectionRequestHandlers.HandleNotification(handler, @params);
            }

            await result;
        }

        public async Task<ErrorResponse> RouteRequest(Request request)
        {
            var id = GetId(request.Id);
            var cts = new CancellationTokenSource();
            _requests.TryAdd(id, cts);

            // TODO: Try / catch for Internal Error
            try
            {
                var method = FindDescriptor(request.Method, request.Params);
                if (method is null)
                {
                    return new MethodNotFound(request.Id);
                }

                Task result;
                if (method.Params is null)
                {
                    result = ReflectionRequestHandlers.HandleRequest(method, cts.Token);
                }
                else
                {
                    object @params;
                    try
                    {
                        @params = request.Params.ToObject(method.Params);
                    }
                    catch
                    {
                        return new InvalidParams(request.Id);
                    }

                    result = ReflectionRequestHandlers.HandleRequest(method, @params, cts.Token);
                }

                await result.ConfigureAwait(false);


                object responseValue = null;
                if (result.GetType().GetTypeInfo().IsGenericType)
                {
                    var property = typeof(Task<>)
                        .MakeGenericType(result.GetType().GetTypeInfo().GetGenericArguments()[0]).GetTypeInfo()
                        .GetProperty(nameof(Task<object>.Result), BindingFlags.Public | BindingFlags.Instance);

                    responseValue = property.GetValue(result);
                }

                return new JsonRpc.Client.Response(request.Id, responseValue);
            }
            catch (TaskCanceledException)
            {
                return new RequestCancelled();
            }
            finally
            {
                _requests.TryRemove(id, out var _);
            }
        }

        public void CancelRequest(object id)
        {
            if (_requests.TryGetValue(GetId(id), out var cts))
            {
                cts.Cancel();
            }
        }
    }
}
