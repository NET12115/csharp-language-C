﻿using System.Threading.Tasks;
using JsonRpc;

// ReSharper disable CheckNamespace

namespace Lsp.Protocol
{
    public static class SendTelemetryExtensions
    {
        public static Task SendTelemetry(this IOutgoingRequestRouter mediator, object @params)
        {
            return mediator.SendNotification("telemetry/event", @params);
        }
    }
}