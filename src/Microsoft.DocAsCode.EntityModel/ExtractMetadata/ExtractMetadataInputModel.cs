namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class ExtractMetadataInputModel
    {
        public Dictionary<string, List<string>> Items { get; set; }

        public string ApiFolderName { get; set; } = "api";

        public string TocFileName { get; set; } = "toc.yml";

        public string IndexFileName { get; set; } = "index.yml";

        public bool PreserveRawInlineComments { get; set; }

        public override string ToString()
        {
            using(StringWriter writer = new StringWriter())
            {
                JsonUtility.Serialize(writer, this);
                return writer.ToString();
            }
        }

        public ExtractMetadataInputModel Clone()
        {
            var cloned = (ExtractMetadataInputModel)this.MemberwiseClone();
            cloned.Items = new Dictionary<string, List<string>>(Items);
            return cloned;
        }
    }
}
