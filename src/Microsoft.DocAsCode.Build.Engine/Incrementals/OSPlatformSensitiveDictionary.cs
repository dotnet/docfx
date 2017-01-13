namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    public class OSPlatformSensitiveDictionary<V> : Dictionary<string, V>
    {
        public OSPlatformSensitiveDictionary() : base(FilePathComparer.OSPlatformSensitiveStringComparer)
        {
        }

        public OSPlatformSensitiveDictionary(IDictionary<string, V> dictionary) : base(dictionary, FilePathComparer.OSPlatformSensitiveStringComparer)
        {
        }
    }
}
