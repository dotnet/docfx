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

        [YamlMember(Alias = "example")]
        public string Example { get; set; }

        [YamlMember(Alias = "syntax")]
        public SyntaxDetailViewModel Syntax { get; set; }

        [YamlMember(Alias = "overridden")]
        public string Overridden { get; set; }

        [YamlMember(Alias = "exceptions")]
        public List<CrefInfo> Exceptions { get; set; }

        [YamlMember(Alias = "seealso")]
        public List<CrefInfo> SeeAlsos { get; set; }

        [YamlMember(Alias = "see")]
        public List<CrefInfo> Sees { get; set; }

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
                Example = model.Example,
                Syntax = SyntaxDetailViewModel.FromModel(model.Syntax),
                Overridden = model.Overridden,
                Exceptions = model.Exceptions,
                Sees = model.Sees,
                SeeAlsos = model.SeeAlsos,
                Inheritance = model.Inheritance,
                Implements = model.Implements,
                InheritedMembers = model.InheritedMembers,
            };

            result.Id = model.Name.Substring((model.Parent?.Name?.Length ?? -1) + 1);

            result.Name = model.DisplayNames.GetLanguageProperty(SyntaxLanguage.Default);
            var nameForCSharp = model.DisplayNames.GetLanguageProperty(SyntaxLanguage.CSharp);
            if (result.Name != nameForCSharp)
            {
                result.NameForCSharp = nameForCSharp;
            }
            var nameForVB = model.DisplayNames.GetLanguageProperty(SyntaxLanguage.VB);
            if (result.Name != nameForVB)
            {
                result.NameForVB = nameForVB;
            }

            result.Fullname = model.DisplayQualifiedNames.GetLanguageProperty(SyntaxLanguage.Default);
            var fullnameForCSharp = model.DisplayQualifiedNames.GetLanguageProperty(SyntaxLanguage.CSharp);
            if (result.Fullname != fullnameForCSharp)
            {
                result.FullnameForCSharp = fullnameForCSharp;
            }
            var fullnameForVB = model.DisplayQualifiedNames.GetLanguageProperty(SyntaxLanguage.VB);
            if (result.Fullname != fullnameForVB)
            {
                result.FullnameForVB = fullnameForVB;
            }

            return result;
        }
    }
}
