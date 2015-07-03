namespace Microsoft.DocAsCode.EntityModel.ViewModels
{
    using YamlDotNet.Serialization;

    public class SpecViewModel
    {
        [YamlMember(Alias = "Uid")]
        public string Uid { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "fullname")]
        public string Fullname { get; set; }

        [YamlMember(Alias = "isExternal")]
        public bool IsExternal { get; set; }

        [YamlMember(Alias = "href")]
        public string Href { get; set; }

        public static SpecViewModel FromModel(LinkItem model)
        {
            if (model == null)
            {
                return null;
            }
            var result = new SpecViewModel
            {
                Uid = model.Name,
                Name = model.DisplayName,
                Fullname = model.DisplayQualifiedNames,
                IsExternal = model.IsExternalPath,
                Href = model.Href,
            };
            return result;
        }
    }
}
