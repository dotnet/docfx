namespace Microsoft.DocAsCode.Metadata
{
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;

    public interface IMetadataDefinition
    {
        string Type { get; }
        bool IsMultiValued { get; }
        bool IsQueryable { get; }
        bool IsRequired { get; }
        bool IsVisible { get; }
        string DisplayName { get; }
        string QueryName { get; }
        List<JValue> ChoiceSet { get; }
        string Description { get; }
    }
}