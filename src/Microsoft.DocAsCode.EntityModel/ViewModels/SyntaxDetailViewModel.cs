namespace Microsoft.DocAsCode.EntityModel.ViewModels
{
    using System.Collections.Generic;
    using YamlDotNet.Serialization;

    public class SyntaxDetailViewModel
    {
        [YamlMember(Alias = "content")]
        public string Content { get; set; }

        [YamlMember(Alias = "content.csharp")]
        public string ContentForCSharp { get; set; }

        [YamlMember(Alias = "content.vb")]
        public string ContentForVB { get; set; }

        [YamlMember(Alias = "parameters")]
        public List<ApiParameter> Parameters { get; set; }

        [YamlMember(Alias = "typeParameters")]
        public List<ApiParameter> TypeParameters { get; set; }

        [YamlMember(Alias = "return")]
        public ApiParameter Return { get; set; }

        public static SyntaxDetailViewModel FromModel(SyntaxDetail model)
        {
            if (model == null)
            {
                return null;
            }
            var result = new SyntaxDetailViewModel
            {
                Parameters = model.Parameters,
                TypeParameters = model.TypeParameters,
                Return = model.Return,
            };
            if (model.Content != null && model.Content.Count > 0)
            {
                result.Content = model.Content.GetLanguageProperty(SyntaxLanguage.Default);
                result.ContentForCSharp = model.Content.GetLanguageProperty(SyntaxLanguage.CSharp);
                result.ContentForVB = model.Content.GetLanguageProperty(SyntaxLanguage.VB);
            }
            return result;
        }

    }
}
