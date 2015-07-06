namespace Microsoft.DocAsCode.EntityModel
{
    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using System;
    using System.Collections.Generic;

    public class ExternalReferenceConverter
    {
        public static IEnumerable<ReferenceViewModel> ToExternalReferenceViewModel(PageViewModel page, Uri baseUrl)
        {
            foreach (var item in page.Items)
            {
                yield return new ReferenceViewModel
                {
                    Uid = item.Uid,
                    Name = item.Name,
                    NameForCSharp = item.NameForCSharp,
                    NameForVB = item.NameForVB,
                    Fullname = item.Fullname,
                    FullnameForCSharp = item.FullnameForCSharp,
                    FullnameForVB = item.FullnameForVB,
                    Summary = item.Summary,
                    Type = item.Type,
                    IsExternal = true,
                    Href = new Uri(new Uri(baseUrl, "api/"), item.Href).ToString(),
                };
            }
        }
    }
}
