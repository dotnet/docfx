using System.Collections.Generic;
using Microsoft.Docs.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TaxonomyConverter
    {
        private const string AllowedDomain = "allowedDomain";

        public static Dictionary<string, string[]> GetTrustedDoamins(string json)
        {
            var taxonomies = JsonConvert.DeserializeObject<Taxonomies>(json) ?? new();
            if (taxonomies.TryGetValue(AllowedDomain, out var taxonomy))
            {
                return taxonomy.NestedTaxonomy.dic;
            }

            return new();
        }

        // TODO: add static method GetHtmlTags()
    }
}
