namespace Microsoft.DocAsCode.EntityModel.ViewModels
{
    using System.Collections.Generic;
    using System.Diagnostics;

    public class TocViewModel
        : List<TocItemViewModel>
    {
        public static TocViewModel FromModel(MetadataItem item)
        {
            if (item == null)
            {
                Debug.Fail("item is null.");
                return null;
            }
            switch (item.Type)
            {
                case MemberType.Toc:
                case MemberType.Namespace:
                    var result = new TocViewModel();
                    foreach (var child in item.Items)
                    {
                        result.Add(TocItemViewModel.FromModel(child));
                    }
                    return result;
                default:
                    return null;
            }
        }

    }
}
