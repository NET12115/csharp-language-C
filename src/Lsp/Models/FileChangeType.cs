using Lsp.Converters;
using Newtonsoft.Json;

namespace Lsp.Models
{
    /// <summary>
    ///  The file event type.
    /// </summary>
    [JsonConverter(typeof(NumberEnumConverter))]
    public enum FileChangeType
    {
        /// <summary>
        ///  The file got created.
        /// </summary>
        Created = 1,
        /// <summary>
        ///  The file got changed.
        /// </summary>
        Changed = 2,
        /// <summary>
        ///  The file got deleted.
        /// </summary>
        Deleted = 3,
    }
}