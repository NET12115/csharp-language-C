using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using OmniSharp.Extensions.JsonRpc.Server;
using OmniSharp.Extensions.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization;
using OmniSharp.Extensions.LanguageServer.Server;
using OmniSharp.Extensions.LanguageServer.Server.Abstractions;
using OmniSharp.Extensions.LanguageServer.Server.Handlers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using HandlerCollection = OmniSharp.Extensions.LanguageServer.Server.HandlerCollection;

namespace Lsp.Tests
{
    public class LspRequestRouterTests
    {
        private readonly TestLoggerFactory _testLoggerFactory;
        private readonly IHandlerMatcherCollection _handlerMatcherCollection = new HandlerMatcherCollection();

        public LspRequestRouterTests(ITestOutputHelper testOutputHelper)
        {
            _testLoggerFactory = new TestLoggerFactory(testOutputHelper);
            var logger = Substitute.For<ILogger>();
            var matcher = Substitute.For<IHandlerMatcher>();
            matcher.FindHandler(Arg.Any<object>(), Arg.Any<IEnumerable<ILspHandlerDescriptor>>())
                .Returns(new List<HandlerDescriptor>());

            _handlerMatcherCollection.Add(matcher);
        }

        [Fact]
        public async Task ShouldRouteToCorrect_Notification()
        {
            var textDocumentSyncHandler = TextDocumentSyncHandlerExtensions.With(DocumentSelector.ForPattern("**/*.cs"));
            textDocumentSyncHandler.Handle(Arg.Any<DidSaveTextDocumentParams>()).Returns(Task.CompletedTask);

            var collection = new HandlerCollection { textDocumentSyncHandler };
            var mediator = new LspRequestRouter(collection, _testLoggerFactory, _handlerMatcherCollection, new Serializer());

            var @params = new DidSaveTextDocumentParams() {
                TextDocument = new TextDocumentIdentifier(new Uri("file:///c:/test/123.cs"))
            };

            var request = new Notification(DocumentNames.DidSave, JObject.Parse(JsonConvert.SerializeObject(@params, new Serializer(ClientVersion.Lsp3).Settings)));

            await mediator.RouteNotification(mediator.GetDescriptor(request), request);

            await textDocumentSyncHandler.Received(1).Handle(Arg.Any<DidSaveTextDocumentParams>());
        }

        [Fact]
        public async Task ShouldRouteToCorrect_Notification_WithManyHandlers()
        {
            var textDocumentSyncHandler = TextDocumentSyncHandlerExtensions.With(DocumentSelector.ForPattern("**/*.cs"));
            var textDocumentSyncHandler2 = TextDocumentSyncHandlerExtensions.With(DocumentSelector.ForPattern("**/*.cake"));
            textDocumentSyncHandler.Handle(Arg.Any<DidSaveTextDocumentParams>()).Returns(Task.CompletedTask);
            textDocumentSyncHandler2.Handle(Arg.Any<DidSaveTextDocumentParams>()).Returns(Task.CompletedTask);

            var collection = new HandlerCollection { textDocumentSyncHandler, textDocumentSyncHandler2 };
            var mediator = new LspRequestRouter(collection, _testLoggerFactory, _handlerMatcherCollection, new Serializer());

            var @params = new DidSaveTextDocumentParams() {
                TextDocument = new TextDocumentIdentifier(new Uri("file:///c:/test/123.cake"))
            };

            var request = new Notification(DocumentNames.DidSave, JObject.Parse(JsonConvert.SerializeObject(@params, new Serializer(ClientVersion.Lsp3).Settings)));

            await mediator.RouteNotification(mediator.GetDescriptor(request), request);

            await textDocumentSyncHandler.Received(1).Handle(Arg.Any<DidSaveTextDocumentParams>());
            await textDocumentSyncHandler2.Received(0).Handle(Arg.Any<DidSaveTextDocumentParams>());
        }

        [Fact]
        public async Task ShouldRouteToCorrect_Request()
        {
            var textDocumentSyncHandler = TextDocumentSyncHandlerExtensions.With(DocumentSelector.ForPattern("**/*.cs"));
            textDocumentSyncHandler.Handle(Arg.Any<DidSaveTextDocumentParams>()).Returns(Task.CompletedTask);

            var codeActionHandler = Substitute.For<ICodeActionHandler>();
            codeActionHandler.GetRegistrationOptions().Returns(new TextDocumentRegistrationOptions() { DocumentSelector = DocumentSelector.ForPattern("**/*.cs") });
            codeActionHandler
                .Handle(Arg.Any<CodeActionParams>(), Arg.Any<CancellationToken>())
                .Returns(new CommandContainer());

            var collection = new HandlerCollection { textDocumentSyncHandler, codeActionHandler };
            var mediator = new LspRequestRouter(collection, _testLoggerFactory, _handlerMatcherCollection, new Serializer());

            var id = Guid.NewGuid().ToString();
            var @params = new DidSaveTextDocumentParams() {
                TextDocument = new TextDocumentIdentifier(new Uri("file:///c:/test/123.cs"))
            };

            var request = new Request(id, DocumentNames.CodeAction, JObject.Parse(JsonConvert.SerializeObject(@params, new Serializer(ClientVersion.Lsp3).Settings)));

            await mediator.RouteRequest(mediator.GetDescriptor(request), request);

            await codeActionHandler.Received(1).Handle(Arg.Any<CodeActionParams>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ShouldRouteToCorrect_Request_WithManyHandlers()
        {
            var textDocumentSyncHandler = TextDocumentSyncHandlerExtensions.With(DocumentSelector.ForPattern("**/*.cs"));
            var textDocumentSyncHandler2 = TextDocumentSyncHandlerExtensions.With(DocumentSelector.ForPattern("**/*.cake"));
            textDocumentSyncHandler.Handle(Arg.Any<DidSaveTextDocumentParams>()).Returns(Task.CompletedTask);
            textDocumentSyncHandler2.Handle(Arg.Any<DidSaveTextDocumentParams>()).Returns(Task.CompletedTask);

            var codeActionHandler = Substitute.For<ICodeActionHandler>();
            codeActionHandler.GetRegistrationOptions().Returns(new TextDocumentRegistrationOptions() { DocumentSelector = DocumentSelector.ForPattern("**/*.cs") });
            codeActionHandler
                .Handle(Arg.Any<CodeActionParams>(), Arg.Any<CancellationToken>())
                .Returns(new CommandContainer());

            var codeActionHandler2 = Substitute.For<ICodeActionHandler>();
            codeActionHandler2.GetRegistrationOptions().Returns(new TextDocumentRegistrationOptions() { DocumentSelector = DocumentSelector.ForPattern("**/*.cake") });
            codeActionHandler2
                .Handle(Arg.Any<CodeActionParams>(), Arg.Any<CancellationToken>())
                .Returns(new CommandContainer());

            var collection = new HandlerCollection { textDocumentSyncHandler, textDocumentSyncHandler2, codeActionHandler, codeActionHandler2 };
            var mediator = new LspRequestRouter(collection, _testLoggerFactory, _handlerMatcherCollection, new Serializer());

            var id = Guid.NewGuid().ToString();
            var @params = new DidSaveTextDocumentParams() {
                TextDocument = new TextDocumentIdentifier(new Uri("file:///c:/test/123.cake"))
            };

            var request = new Request(id, DocumentNames.CodeAction, JObject.Parse(JsonConvert.SerializeObject(@params, new Serializer(ClientVersion.Lsp3).Settings)));

            await mediator.RouteRequest(mediator.GetDescriptor(request), request);

            await codeActionHandler.Received(1).Handle(Arg.Any<CodeActionParams>(), Arg.Any<CancellationToken>());
            await codeActionHandler2.Received(0).Handle(Arg.Any<CodeActionParams>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ShouldHandle_Request_WithNullParameters()
        {
            bool wasShutDown = false;

            ShutdownHandler shutdownHandler = new ShutdownHandler();
            shutdownHandler.Shutdown += shutdownRequested =>
            {
                wasShutDown = true;
            };

            var collection = new HandlerCollection { shutdownHandler };
            var mediator = new LspRequestRouter(collection, _testLoggerFactory, _handlerMatcherCollection, new Serializer());

            JToken @params = JValue.CreateNull(); // If the "params" property present but null, this will be JTokenType.Null.

            var id = Guid.NewGuid().ToString();
            var request = new Request(id, GeneralNames.Shutdown, @params);

            await mediator.RouteRequest(mediator.GetDescriptor(request), request);

            Assert.True(wasShutDown, "WasShutDown");
        }

        [Fact]
        public async Task ShouldHandle_Request_WithMissingParameters()
        {
            bool wasShutDown = false;

            ShutdownHandler shutdownHandler = new ShutdownHandler();
            shutdownHandler.Shutdown += shutdownRequested =>
            {
                wasShutDown = true;
            };

            var collection = new HandlerCollection { shutdownHandler };
            var mediator = new LspRequestRouter(collection, _testLoggerFactory, _handlerMatcherCollection, new Serializer());

            JToken @params = null; // If the "params" property was missing entirely, this will be null.

            var id = Guid.NewGuid().ToString();
            var request = new Request(id, GeneralNames.Shutdown, @params);

            await mediator.RouteRequest(mediator.GetDescriptor(request), request);

            Assert.True(wasShutDown, "WasShutDown");
        }
    }
}
