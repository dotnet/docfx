using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.DocAsCode.Metadata.SchemaValidators
{
    public static class ValidatorFactory
    {
        public static IWellknownMetadataValidator Then(this IWellknownMetadataValidator validator, IWellknownMetadataValidator next)
        {
            var chain = validator as ChainWellknownValidator;
            if (chain == null)
            {
                return new ChainWellknownValidator
                {
                    Validators =
                    {
                        validator,
                        next,
                    }
                };
            }
            chain.Validators.Add(next);
            return chain;
        }

        private sealed class ChainWellknownValidator : IWellknownMetadataValidator
        {
            public List<IWellknownMetadataValidator> Validators { get; } = new List<IWellknownMetadataValidator>();

            public ValidationResult Validate(string path, IMetadataDefinition definition, JToken value)
            {
                foreach (var v in Validators)
                {
                    var result = v.Validate(path, definition, value);
                    if (result != ValidationResult.Success)
                    {
                        return result;
                    }
                }
                return ValidationResult.Success;
            }
        }
    }
}
