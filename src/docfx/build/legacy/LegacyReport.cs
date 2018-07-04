// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class LegacyReport
    {
        public static void Convert(Docset docset, Report report)
        {
            report.Dispose();

            var reportPath = Path.Combine(docset.Config.Output.Path, "build.log");

            if (File.Exists(reportPath))
            {
                File.WriteAllLines(reportPath, File.ReadAllLines(reportPath).Select(Convert));
            }
        }

        private static string Convert(string str)
        {
            var arr = JArray.Parse(str);
            var message_severity = arr.Count > 0 ? arr.Value<string>(0) : null;
            var code = arr.Count > 1 ? arr.Value<string>(1) : null;
            var message = arr.Count > 2 ? arr.Value<string>(2) : null;
            var file = arr.Count > 3 ? arr.Value<string>(3) : null;
            var line = arr.Count > 4 ? arr.Value<int?>(4) : null;

            return JsonUtility.Serialize(new { message_severity, code, message, file, line });
        }
    }
}
