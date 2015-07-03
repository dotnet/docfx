namespace Microsoft.DocAsCode.EntityModel.ViewModels
{
    using System.Collections.Generic;
    using YamlDotNet.Serialization;

    public class PageViewModel
    {
        [YamlMember(Alias = "items")]
        public List<ItemViewModel> Items { get; set; } = new List<ItemViewModel>();

        [YamlMember(Alias = "references")]
        public List<ReferenceViewModel> References { get; set; } = new List<ReferenceViewModel>();

        public static PageViewModel FromModel(MetadataItem model)
        {
            if (model == null)
            {
                return null;
            }
            var result = new PageViewModel();
            result.Items.Add(ItemViewModel.FromModel(model));
            if (model.Type.AllowMultipleItems())
            {
                AddChildren(model, result);
            }
            foreach (var item in model.References)
            {
                result.References.Add(ReferenceViewModel.FromModel(item));
            }
            return result;
        }

        private static void AddChildren(MetadataItem model, PageViewModel result)
        {
            if (model.Items != null && model.Items.Count > 0)
            {
                foreach (var item in model.Items)
                {
                    result.Items.Add(ItemViewModel.FromModel(model));
                    AddChildren(item, result);
                }
            }
        }
    }
}
