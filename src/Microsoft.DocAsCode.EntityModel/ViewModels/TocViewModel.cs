// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    [Serializable]
    public class TocViewModel
        : List<TocItemViewModel>
    {
        public TocViewModel(IEnumerable<TocItemViewModel> items) : base(items) { }
        public TocViewModel() : base() { }
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
                    var result = new List<TocItemViewModel>();
                    foreach (var child in item.Items)
                    {
                        result.Add(TocItemViewModel.FromModel(child));
                    }
                    return new TocViewModel(result.OrderBy(s => s.Name));
                default:
                    return null;
            }
        }

    }
}
