namespace Microsoft.DocAsCode.EntityModel
{
    using Microsoft.DocAsCode.Utility;

    public class SourceDetail
    {
        [YamlDotNet.Serialization.YamlMember(Alias = "remote")]
        public GitDetail Remote { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "base")]
        public string BasePath { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "id")]
        public string Name { get; set; }

        /// <summary>
        /// The url path for current source, should be resolved at some late stage
        /// </summary>
        [YamlDotNet.Serialization.YamlMember(Alias = "href")]
        public string Href { get; set; }

        /// <summary>
        /// The local path for current source, should be resolved to be relative path at some late stage
        /// </summary>
        [YamlDotNet.Serialization.YamlMember(Alias = "path")]
        public string Path { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "startLine")]
        public int StartLine { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "endLine")]
        public int EndLine { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "content")]
        public string Content { get; set; }

        /// <summary>
        /// The external path for current source if it is not locally available
        /// </summary>
        [YamlDotNet.Serialization.YamlMember(Alias = "isExternal")]
        public bool IsExternalPath { get; set; }
    }
}
