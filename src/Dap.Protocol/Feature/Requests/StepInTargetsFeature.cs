using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OmniSharp.Extensions.DebugAdapter.Protocol.Models;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.JsonRpc.Generation;

// ReSharper disable once CheckNamespace
namespace OmniSharp.Extensions.DebugAdapter.Protocol
{
    namespace Requests
    {
        [Parallel]
        [Method(RequestNames.StepInTargets, Direction.ClientToServer)]
        [
            GenerateHandler,
            GenerateHandlerMethods,
            GenerateRequestMethods
        ]
        public class StepInTargetsArguments : IRequest<StepInTargetsResponse>
        {
            /// <summary>
            /// The stack frame for which to retrieve the possible stepIn targets.
            /// </summary>
            public long FrameId { get; set; }
        }

        public class StepInTargetsResponse
        {
            /// <summary>
            /// The possible stepIn targets of the specified source location.
            /// </summary>
            public Container<StepInTarget>? Targets { get; set; }
        }
    }

    namespace Models
    {
        /// <summary>
        /// A StepInTarget can be used in the ‘stepIn’ request and determines into which single target the stepIn request should step.
        /// </summary>
        public class StepInTarget
        {
            /// <summary>
            /// Unique identifier for a stepIn target.
            /// </summary>
            public long Id { get; set; }

            /// <summary>
            /// The name of the stepIn target (shown in the UI).
            /// </summary>
            public string Label { get; set; } = null!;
        }
    }
}
