// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.EntityModel
{
    public class ApiReferenceViewModel : Dictionary<string, string>
    {

    }

    public class ApiReferenceModel : Dictionary<string, ApiIndexItemModel>
    {
        public ApiReferenceViewModel ToViewModel()
        {
            ApiReferenceViewModel viewModel = new ApiReferenceViewModel();
            foreach(var item in this)
            {
                viewModel[item.Value.Name] = item.Value.Href;
            }
            return viewModel;
        }
    }

    public class ApiIndexItemModel
    {
        public string Name { get; set; }
        public string IndexFilePath { get; set; }
        public string Href { get; set; }
    }
}
