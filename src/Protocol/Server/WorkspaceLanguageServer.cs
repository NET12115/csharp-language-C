using System;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Progress;

namespace OmniSharp.Extensions.LanguageServer.Protocol.Server
{
    internal class WorkspaceLanguageServer : LanguageProtocolProxy, IWorkspaceLanguageServer
    {
        public WorkspaceLanguageServer(
            IResponseRouter requestRouter, IServiceProvider serviceProvider, IProgressManager progressManager,
            ILanguageProtocolSettings languageProtocolSettings
        ) : base(requestRouter, serviceProvider, progressManager, languageProtocolSettings)
        {
        }
    }
}
