﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using JsonRpc.Server;
using NSubstitute;
using Xunit;

namespace JsonRpc.Tests
{
    public class MediatorTestsNotificationHandler
    {
        [Method("exit")]
        public interface IExitHandler : INotificationHandler { }

        [Fact]
        public async Task Test1()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            var exitHandler = Substitute.For<IExitHandler>();
            serviceProvider
                .GetService(typeof(IExitHandler))
                .Returns(exitHandler);

            var mediator = new Mediator(new HandlerResolver(typeof(HandlerResolverTests).GetTypeInfo().Assembly), serviceProvider);

            var notification = new Notification("exit", null);

            mediator.HandleNotification(notification);

            await exitHandler.Received(1).Handle();
        }

    }
}