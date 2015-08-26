namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System.Collections.Generic;

    public sealed class YamlConverterContext : IHasUidIndex
    {
        public Dictionary<string, HashSet<FileAndType>> UidIndex { get; set; }

        public Dictionary<string, UidTreeNode> UidTree { get; set; }
    }
}
