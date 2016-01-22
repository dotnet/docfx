namespace Microsoft.DocAsCode.Metadata
{
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;

    using Microsoft.DocAsCode.Metadata.SchemaValidators;

    public static class MetadataParser
    {
        private static readonly IMetadataSchema Schema =
            new MetadataSchema(
                unknownValidators: new List<IUnknownMetadataValidator>
                {
                    new UnknownNamingValidator(),
                    new DefinitionObjectValidator(),
                });

        public static IMetadataSchema GetMetadataSchema()
        {
            return Schema;
        }

        public static IMetadataSchema Load(string content) =>
            new MetadataSchema(
                JsonConvert.DeserializeObject<Dictionary<string, MetadataDefinition>>(content)
                .ToDictionary(
                    x => x.Key,
                    x => (IMetadataDefinition)x.Value),
                new List<IWellknownMetadataValidator>
                {
                    new WellknownTypeValidator()
                        .Then(new WellknownChoiceSetValidator()),
                },
                new List<IUnknownMetadataValidator>
                {
                    new UnknownNamingValidator(),
                    new UnknownTypeValidator(),
                });
    }
}
