using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JoinTOCConfig
    {
        public string? OutputPath { get; set; }

        public JObject? ContainerPageMetadata { get; set; }

        public string? ReferenceToc { get; set; }

        public string? TopLevelToc { get; set; }
    }
}
