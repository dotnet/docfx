using System.Collections.Generic;
using Microsoft.Docs.Validation;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class TaxonomyProvider
    {
        private readonly FileResolver _fileResolver;
        private readonly Config _config;
        private readonly Taxonomies _taxonomies;

        public TaxonomyProvider(Config config, FileResolver fileResolver)
        {
            _fileResolver = fileResolver;
            _config = config;
            _taxonomies = LoadTaxonomies();
        }

        public static Dictionary<string, TrustedDomains> GetTrustedDomains()
        {
            // TODO: need to convert taxonomy 'allowDomains' here
            return new();
        }

        private Taxonomies LoadTaxonomies()
        {
            return LoadTaxonomies(_fileResolver.ReadString(_config.Allowlists));
        }

        private static Taxonomies LoadTaxonomies(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new();
            }

            return JsonConvert.DeserializeObject<Taxonomies>(json) ?? new();
        }
    }
}
