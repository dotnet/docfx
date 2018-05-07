using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    public class TestSpec
    {
        public string Path;

        public readonly Dictionary<string, string> Inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public readonly Dictionary<string, string> Outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public readonly Dictionary<string, string> Restorations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public readonly Dictionary<string, string> Exceptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public override string ToString() => Path;
    }
}
