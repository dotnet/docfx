// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tools.GEmojiCodeGenerators
{
    using System.IO;
    using System.Net;
    using System.Text;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class Program
    {
        public static void Main()
        {
            using (var fs = File.Create("GfmEmojiInlineRule.generated.cs"))
            using (var sw = new StreamWriter(fs))
            {
                sw.WriteLine("// Copyright (c) Microsoft. All rights reserved.");
                sw.WriteLine("// Licensed under the MIT license. See LICENSE file in the project root for full license information.");
                sw.WriteLine();
                sw.WriteLine("namespace Microsoft.DocAsCode.MarkdownLite");
                sw.WriteLine("{");
                sw.WriteLine("    using System.Collections.Generic;");
                sw.WriteLine();
                sw.WriteLine("    public partial class GfmEmojiInlineRule");
                sw.WriteLine("    {");
                sw.WriteLine("        // from https://raw.githubusercontent.com/github/gemoji/master/db/emoji.json");
                sw.WriteLine("        private static Dictionary<string, string> LoadEmoji() =>");
                sw.WriteLine("            new Dictionary<string, string>");
                sw.WriteLine("            {");

                foreach (JObject obj in LoadEmojiJson())
                {
                    var emojiProperty = obj.Property("emoji");
                    if (emojiProperty == null)
                    {
                        continue;
                    }
                    var emoji = (emojiProperty.Value as JValue)?.ToString();
                    if (emoji == null)
                    {
                        continue;
                    }
                    var aliasesProperty = obj.Property("aliases");
                    if (aliasesProperty == null)
                    {
                        continue;
                    }
                    var aliases = aliasesProperty.Value as JArray;
                    if (aliases == null || aliases.Count == 0)
                    {
                        continue;
                    }
                    foreach (var aliase in aliases)
                    {
                        sw.WriteLine($"                [\"{aliase.ToString()}\"] = \"{emoji}\",");
                    }
                }

                sw.WriteLine("            };");
                sw.WriteLine("    }");
                sw.WriteLine("}");
            }
        }

        private static JArray LoadEmojiJson()
        {
            using (var wc = new WebClient())
            {
                var bytes = wc.DownloadData("https://raw.githubusercontent.com/github/gemoji/master/db/emoji.json");
                var json = Encoding.UTF8.GetString(bytes);
                return JsonConvert.DeserializeObject<JArray>(json);
            }
        }
    }
}