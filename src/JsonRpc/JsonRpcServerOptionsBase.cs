using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nerdbank.Streams;

namespace OmniSharp.Extensions.JsonRpc
{
    public abstract class JsonRpcServerOptionsBase<T> : JsonRpcOptionsRegistryBase<T>, IJsonRpcServerOptions where T : IJsonRpcHandlerRegistry<T>
    {
        protected JsonRpcServerOptionsBase()
        {
            WithAssemblies(typeof(JsonRpcServer).Assembly);
        }
        public PipeReader? Input { get; set; }
        public PipeWriter? Output { get; set; }

        public ILoggerFactory LoggerFactory
        {
            get => Services.FirstOrDefault(z => z.ServiceType == typeof(ILoggerFactory))?.ImplementationInstance as ILoggerFactory ?? NullLoggerFactory.Instance;
            set => WithLoggerFactory(value);
        }

        public IEnumerable<Assembly> Assemblies { get; set; } = Enumerable.Empty<Assembly>();
        /// <summary>
        /// Experimental support for using assembly attributes
        /// </summary>
        public bool UseAssemblyAttributeScanning { get; set; } = false;
        public IRequestProcessIdentifier? RequestProcessIdentifier { get; set; }
        public int? Concurrency { get; set; }
        public IScheduler InputScheduler { get; set; } = TaskPoolScheduler.Default;
        public IScheduler OutputScheduler { get; set; } = Scheduler.Immediate;
        public IScheduler DefaultScheduler { get; set; } = TaskPoolScheduler.Default;
        public CreateResponseExceptionHandler? CreateResponseException { get; set; }
        public OnUnhandledExceptionHandler? OnUnhandledException { get; set; }
        public bool SupportsContentModified { get; set; } = true;
        public TimeSpan MaximumRequestTimeout { get; set; } = TimeSpan.FromMinutes(5);
        internal CompositeDisposable CompositeDisposable { get; } = new CompositeDisposable();
        IDisposable IJsonRpcServerOptions.RegisteredDisposables => CompositeDisposable;

        public void RegisterForDisposal(IDisposable disposable) => CompositeDisposable.Add(disposable);

        public T WithAssemblies(IEnumerable<Assembly>? assemblies)
        {
            Assemblies = Assemblies.Union(assemblies ?? Enumerable.Empty<Assembly>()).ToArray();
            return (T) (object) this;
        }

        public T WithAssemblies(params Assembly[] assemblies)
        {
            Assemblies = Assemblies.Union(assemblies).ToArray();
            return (T) (object) this;
        }

        public T WithInput(Stream input)
        {
            Input = input.UsePipeReader();
            RegisterForDisposal(input);
            return (T) (object) this;
        }

        public T WithInput(PipeReader input)
        {
            Input = input;
            return (T) (object) this;
        }

        public T WithOutput(Stream output)
        {
            Output = output.UsePipeWriter();
            RegisterForDisposal(output);
            return (T) (object) this;
        }

        public T WithOutput(PipeWriter output)
        {
            Output = output;
            return (T) (object) this;
        }

        public T WithPipe(Pipe pipe)
        {
            Input = pipe.Reader;
            Output = pipe.Writer;
            return (T) (object) this;
        }

        public T WithLoggerFactory(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == NullLoggerFactory.Instance) return (T) (object) this;
            Services.RemoveAll(typeof(ILoggerFactory));
            Services.AddSingleton(loggerFactory);
            return (T) (object) this;
        }

        public T WithRequestProcessIdentifier(IRequestProcessIdentifier requestProcessIdentifier)
        {
            RequestProcessIdentifier = requestProcessIdentifier;
            return (T) (object) this;
        }

        public T WithHandler<THandler>(JsonRpcHandlerOptions? options = null)
            where THandler : class, IJsonRpcHandler =>
            AddHandler<THandler>(options);

        public T WithHandlersFrom(Type type, JsonRpcHandlerOptions? options = null) => AddHandler(type, options);

        public T WithHandlersFrom(TypeInfo typeInfo, JsonRpcHandlerOptions? options = null) => AddHandler(typeInfo.AsType(), options);

        public T WithResponseExceptionFactory(CreateResponseExceptionHandler handler)
        {
            CreateResponseException = handler;
            return (T) (object) this;
        }

        public T WithUnhandledExceptionHandler(OnUnhandledExceptionHandler handler)
        {
            OnUnhandledException = handler;
            return (T) (object) this;
        }

        public T WithContentModifiedSupport(bool supportsContentModified)
        {
            SupportsContentModified = supportsContentModified;
            return (T) (object) this;
        }

        public T WithMaximumRequestTimeout(TimeSpan maximumRequestTimeout)
        {
            MaximumRequestTimeout = maximumRequestTimeout;
            return (T) (object) this;
        }

        public T WithLink(string fromMethod, string toMethod)
        {
            Handlers.Add(JsonRpcHandlerDescription.Link(fromMethod, toMethod));
            return (T) (object) this;
        }

        public T WithActivityTracingStrategy(IActivityTracingStrategy activityTracingStrategy)
        {
            Services.RemoveAll(typeof(IActivityTracingStrategy));
            Services.AddSingleton(activityTracingStrategy);
            return (T) (object) this;
        }
    }
}
