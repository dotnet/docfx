using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal interface IXrefSpec
    {
        string Uid { get; set; }

        string Href { get; set; }

        Document ReferencedFile { get; set; }

        HashSet<string> Monikers { get; set; }

        string GetXrefPropertyValue(string propertyName);

        string GetName();
    }
}
