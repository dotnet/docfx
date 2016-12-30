// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.JavaScriptReference
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.DataContracts.Common;

    using AutoMapper;

    public static class AutoMapperHelper
    {
        public static ApiBuildOutput ToApiBuildOutput(this PageViewModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            if (model.Items == null || model.Items.Count == 0)
            {
                throw new ArgumentException($"{nameof(model)} must contain at least one item");
            }

            var supportedLanguages = model.Items?[0].SupportedLanguages ?? new[] { Constants.JavaScriptDevLang };

            // map references
            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile(new ApiReferenceBuildOutputProfile(supportedLanguages));
            });
            Dictionary<string, ApiReferenceBuildOutput> references = null;
            if (model.References != null)
            {
                references = new Dictionary<string, ApiReferenceBuildOutput>();
                foreach (var reference in model.References
                    .Where(r => !string.IsNullOrEmpty(r.Uid))
                    .Select(Mapper.Map<ReferenceViewModel, ApiReferenceBuildOutput>))
                {
                    references[reference.Uid] = reference;
                }
            }

            // map items
            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile(new ApiBuildOutputProfile(supportedLanguages, model.Metadata, references));
            });
            var items = Mapper.Map<List<ItemViewModel>, List<ApiBuildOutput>>(model.Items);
            var result = items[0];
            if (model.Items[0].Children == null) return result;
            result.Children = new List<ApiBuildOutput>();
            foreach (var child in model.Items[0].Children
                .Select(c => items.Find(i => i.Uid == c)))
            {
                result.Children.Add(child);
            }
            return result;
        }
    }
}
