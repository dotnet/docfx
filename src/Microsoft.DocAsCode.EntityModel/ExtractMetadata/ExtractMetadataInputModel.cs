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
            return new ExtractMetadataInputModel
            {
                ApiFolderName = ApiFolderName,
                TocFileName = TocFileName,
                IndexFileName = IndexFileName,
                Items = new Dictionary<string, List<string>>(Items)
            };
        }
    }
}
