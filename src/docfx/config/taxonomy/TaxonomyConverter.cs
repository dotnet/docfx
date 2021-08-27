// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.Docs.Validation;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class TaxonomyConverter
    {
        private const string AllowedDomain = "allowedDomain";
        private const string AllowedHTML = "allowedHTML";

        public static Dictionary<string, string[]> GetTrustedDoamins(string json)
        {
            var taxonomies = JsonConvert.DeserializeObject<Taxonomies>(json) ?? new();
            if (taxonomies.TryGetValue(AllowedDomain, out var taxonomy))
            {
                return taxonomy.NestedTaxonomy.dic;
            }

            return new();
        }

        public static Dictionary<string, string[]> GetAllowedHTML(string json)
        {
            var taxonomies = JsonConvert.DeserializeObject<Taxonomies>(json) ?? new();
            if (taxonomies.TryGetValue(AllowedHTML, out var taxonomy))
            {
                return taxonomy.NestedTaxonomy.dic; // todo remove (empty)
            }

            return new();
        }
    }
}
