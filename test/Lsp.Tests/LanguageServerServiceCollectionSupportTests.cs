using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Server;
using Xunit;
using Xunit.Abstractions;

namespace Lsp.Tests
{
    public class LanguageServerServiceCollectionSupportTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public LanguageServerServiceCollectionSupportTests(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

        [Fact]
        public void Inner_Services_Should_Override_Outer_Services()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            var services = new ServiceCollection()
                          .AddLanguageServer(
                               options => {
                                   var pipe = new Pipe();
                                   options
                                      .WithInput(pipe.Reader)
                                      .WithOutput(pipe.Writer)
                                      .WithServices(s => s.AddSingleton(new OutsideService("override")))
                                      .AddHandler<Handler>(new JsonRpcHandlerOptions { RequestProcessType = RequestProcessType.Serial });
                               }
                           )
                          .AddSingleton(new OutsideService("servername"))
                          .AddSingleton<ILoggerFactory>(new TestLoggerFactory(_testOutputHelper))
                          .BuildServiceProvider();

            using var server = services.GetRequiredService<LanguageServer>();
            server.GetRequiredService<OutsideService>().Value.Should().Be("override");
        }

        [Fact]
        public void Handlers_Can_Be_Added_From_The_Service_Collection()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            var services = new ServiceCollection()
                          .AddLanguageServer(
                               options => {
                                   var pipe = new Pipe();
                                   options
                                      .WithInput(pipe.Reader)
                                      .WithOutput(pipe.Writer)
                                      .WithServices(
                                           serviceCollection =>
                                               serviceCollection.AddJsonRpcHandler<Handler>(new JsonRpcHandlerOptions { RequestProcessType = RequestProcessType.Serial })
                                       );
                               }
                           )
                          .AddSingleton(new OutsideService("servername"))
                          .AddSingleton<ILoggerFactory>(new TestLoggerFactory(_testOutputHelper))
                          .BuildServiceProvider();

            using var server = services.GetRequiredService<LanguageServer>();
            server.HandlersManager.Descriptors.Should().Contain(z => z.Handler.GetType() == typeof(Handler));
        }

        [Fact]
        public void Should_Bootstrap_Multiple_Servers_Through_Service_Collection()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            var services = new ServiceCollection()
                          .AddLanguageServer(
                               "serial", options => {
                                   var pipe = new Pipe();
                                   options
                                      .WithInput(pipe.Reader)
                                      .WithOutput(pipe.Writer)
                                      .AddHandler<Handler>(new JsonRpcHandlerOptions { RequestProcessType = RequestProcessType.Serial });
                               }
                           )
                          .AddLanguageServer(
                               "parallel", options => {
                                   var pipe = new Pipe();
                                   options
                                      .WithInput(pipe.Reader)
                                      .WithOutput(pipe.Writer)
                                      .AddHandler<Handler>(new JsonRpcHandlerOptions { RequestProcessType = RequestProcessType.Parallel });
                               }
                           )
                          .AddSingleton(new OutsideService("outside"))
                          .AddSingleton<ILoggerFactory>(new TestLoggerFactory(_testOutputHelper))
                          .BuildServiceProvider();

            var resolver = services.GetRequiredService<LanguageServerResolver>();
            var serialServer = resolver.Get("serial").Should().NotBeNull().And.Subject;
            var parallelServer = resolver.Get("parallel").Should().NotBeNull().And.Subject;
            serialServer.Should().NotBe(parallelServer);

            resolver.Get("serial").Should().Be(serialServer);
            resolver.Get("parallel").Should().Be(parallelServer);
        }

        [Fact]
        public void Should_Throw_When_Multiple_Servers_Are_Added_And_Attempt_To_Resolve_Server()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var services = new ServiceCollection()
                          .AddLanguageServer(
                               "serial", options => {
                                   var pipe = new Pipe();
                                   options
                                      .WithInput(pipe.Reader)
                                      .WithOutput(pipe.Writer)
                                      .AddHandler<Handler>(new JsonRpcHandlerOptions { RequestProcessType = RequestProcessType.Serial });
                               }
                           )
                          .AddLanguageServer(
                               "parallel", options => {
                                   var pipe = new Pipe();
                                   options
                                      .WithInput(pipe.Reader)
                                      .WithOutput(pipe.Writer)
                                      .AddHandler<Handler>(new JsonRpcHandlerOptions { RequestProcessType = RequestProcessType.Parallel });
                               }
                           )
                          .AddSingleton<ILoggerFactory>(new TestLoggerFactory(_testOutputHelper))
                          .BuildServiceProvider();

            Action a = () => services.GetRequiredService<LanguageServer>();
            a.Should().Throw<NotSupportedException>();
        }

        [Method("outside")]
        private class Request : IRequest<Response>
        {
        }

        private class Response
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string Value { get; }

            public Response(string value) => Value = value;
        }

        private class Handler : IJsonRpcRequestHandler<Request, Response>
        {
            private readonly OutsideService _outsideService;

            public Handler(OutsideService outsideService) => _outsideService = outsideService;

            public Task<Response> Handle(Request request, CancellationToken cancellationToken) => Task.FromResult(new Response(_outsideService.Value));
        }

        private class OutsideService
        {
            public OutsideService(string value) => Value = value;

            public string Value { get; }
        }
    }
}
