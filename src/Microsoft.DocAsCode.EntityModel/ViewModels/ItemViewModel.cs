namespace Microsoft.DocAsCode.EntityModel.ViewModels
{
    using System.Collections.Generic;
    using YamlDotNet.Serialization;

    public class ItemViewModel
    {
        [YamlMember(Alias = "uid")]
        public string Uid { get; set; }

        [YamlMember(Alias = "id")]
        public string Id { get; set; }

        [YamlMember(Alias = "parent")]
        public string Parent { get; set; }

        [YamlMember(Alias = "children")]
        public List<string> Children { get; set; }

        [YamlMember(Alias = "href")]
        public string Href { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "name.csharp")]
        public string NameForCSharp { get; set; }

        [YamlMember(Alias = "name.vb")]
        public string NameForVB { get; set; }

        [YamlMember(Alias = "fullname")]
        public string Fullname { get; set; }

        [YamlMember(Alias = "fullname.csharp")]
        public string FullnameForCSharp { get; set; }

        [YamlMember(Alias = "fullname.vb")]
        public string FullnameForVB { get; set; }

        [YamlMember(Alias = "Type")]
        public MemberType? Type { get; set; }

        [YamlMember(Alias = "source")]
        public SourceDetail Source { get; set; }

        [YamlMember(Alias = "documentation")]
        public SourceDetail Documentation { get; set; }

        [YamlMember(Alias = "assemblies")]
        public List<string> AssemblyNameList { get; set; }

        [YamlMember(Alias = "namespace")]
        public string NamespaceName { get; set; }

        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "remarks")]
        public string Remarks { get; set; }

        [YamlMember(Alias = "syntax")]
        public SyntaxDetailViewModel Syntax { get; set; }

        [YamlMember(Alias = "overridden")]
        public string Overridden { get; set; }

        [YamlMember(Alias = "exceptions")]
        public List<ExceptionDetail> Exceptions { get; set; }

        [YamlMember(Alias = "inheritance")]
        public List<string> Inheritance { get; set; }

        [YamlMember(Alias = "implements")]
        public List<string> Implements { get; set; }

        [YamlMember(Alias = "inheritedMembers")]
        public List<string> InheritedMembers { get; set; }

        public static ItemViewModel FromModel(MetadataItem model)
        {
            if (model == null)
            {
                return null;
            }
            var result = new ItemViewModel
            {
                Uid = model.Name,
                //Id = model.Name,
                Parent = model.Parent?.Name,
                Children = model.Items?.ConvertAll(x => x.Name),
                Href = model.Href,
                Type = model.Type,
                Source = model.Source,
                Documentation = model.Documentation,
                AssemblyNameList = model.AssemblyNameList,
                NamespaceName = model.NamespaceName,
                Summary = model.Summary,
                Remarks = model.Remarks,
                Syntax = SyntaxDetailViewModel.FromModel(model.Syntax),
                Overridden = model.Overridden,
                Exceptions = model.Exceptions,
                Inheritance = model.Inheritance,
                Implements = model.Implements,
                InheritedMembers = model.InheritedMembers,
            };

            result.Name = model.DisplayNames.GetLanguageProperty(SyntaxLanguage.Default);
            result.NameForCSharp = model.DisplayNames.GetLanguageProperty(SyntaxLanguage.CSharp);
            result.NameForVB = model.DisplayNames.GetLanguageProperty(SyntaxLanguage.VB);

            result.Fullname = model.DisplayQualifiedNames.GetLanguageProperty(SyntaxLanguage.Default);
            result.FullnameForCSharp = model.DisplayQualifiedNames.GetLanguageProperty(SyntaxLanguage.CSharp);
            result.FullnameForVB = model.DisplayQualifiedNames.GetLanguageProperty(SyntaxLanguage.VB);

            return result;
        }
    }
}
