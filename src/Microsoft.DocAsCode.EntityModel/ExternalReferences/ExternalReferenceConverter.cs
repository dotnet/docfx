// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using System;
    using System.Collections.Generic;

    public class ExternalReferenceConverter
    {
        public static IEnumerable<ReferenceViewModel> ToExternalReferenceViewModel(PageViewModel page, Uri baseUri)
        {
            foreach (var item in page.Items)
            {
                yield return new ReferenceViewModel
                {
                    Uid = item.Uid,
                    Name = item.Name,
                    NameForCSharp = item.NameForCSharp,
                    NameForVB = item.NameForVB,
                    FullName = item.FullName,
                    FullNameForCSharp = item.FullNameForCSharp,
                    FullNameForVB = item.FullNameForVB,
                    Href = baseUri + "/" + item.Href
                };
            }
        }
    }
}
