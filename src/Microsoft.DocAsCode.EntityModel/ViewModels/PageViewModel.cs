// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.ViewModels
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    [Serializable]
    public class PageViewModel
    {
        [YamlMember(Alias = "items")]
        [JsonProperty("items")]
        public List<ItemViewModel> Items { get; set; } = new List<ItemViewModel>();

        [YamlMember(Alias = "references")]
        [JsonProperty("references")]
        public List<ReferenceViewModel> References { get; set; } = new List<ReferenceViewModel>();

        [YamlMember(Alias = "metadata")]
        [JsonProperty("metadata")]
        public Dictionary<string, object> Metadata { get; set; }

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
                    result.Items.Add(ItemViewModel.FromModel(item));
                    AddChildren(item, result);
                }
            }
        }
    }
}
