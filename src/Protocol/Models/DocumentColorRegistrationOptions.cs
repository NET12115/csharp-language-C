using OmniSharp.Extensions.LanguageServer.Protocol.Serialization;

namespace OmniSharp.Extensions.LanguageServer.Protocol.Models
{
    public class DocumentColorRegistrationOptions : WorkDoneTextDocumentRegistrationOptions, IDocumentColorOptions
    {
        /// <summary>
        ///  Code lens has a resolve provider as well.
        /// </summary>
        [Optional]
        public bool ResolveProvider { get; set; }
    }
}
