namespace Microsoft.DocAsCode.EntityModel.ViewModels
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using YamlDotNet.Serialization;

    public class TocItemViewModel
    {
        [YamlMember(Alias = "uid")]
        public string Uid { get; set; }
        [YamlMember(Alias = "name")]
        public string Name { get; set; }
        [YamlMember(Alias = "name.csharp")]
        public string NameForCSharp { get; set; }
        [YamlMember(Alias = "name.vb")]
        public string NameForVB { get; set; }
        [YamlMember(Alias = "href")]
        public string Href { get; set; }
        [YamlMember(Alias = "items")]
        public TocViewModel Items { get; set; }

        public static TocItemViewModel FromModel(MetadataItem item)
        {
            var result = new TocItemViewModel
            {
                Uid = item.Name,
                Name = item.DisplayNames.GetLanguageProperty(SyntaxLanguage.Default),
                Href = item.Href,
            };
            var nameForCSharp = item.DisplayNames.GetLanguageProperty(SyntaxLanguage.CSharp);
            if (nameForCSharp != result.Name)
            {
                result.NameForCSharp = nameForCSharp;
            }
            var nameForVB = item.DisplayNames.GetLanguageProperty(SyntaxLanguage.VB);
            if (nameForVB != result.Name)
            {
                result.NameForVB = nameForVB;
            }
            if (item.Items != null)
            {
                result.Items = TocViewModel.FromModel(item);
            }
            return result;
        }
    }
}
