using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;
using OmniSharp.Extensions.JsonRpc;

namespace OmniSharp.Extensions.DebugAdapter.Protocol.Client
{
    /// <summary>
    /// Gives your class or handler an opportunity to interact with
    /// the <see cref="InitializeRequestArguments" /> and <see cref="InitializeResponse" /> before it is processed by the client.
    /// </summary>
    public interface IOnDebugAdapterClientInitialized : IEventingHandler
    {
        Task OnInitialized(IDebugAdapterClient client, InitializeRequestArguments request, InitializeResponse result, CancellationToken cancellationToken);
    }
}
