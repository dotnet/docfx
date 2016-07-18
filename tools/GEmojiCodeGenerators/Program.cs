// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tools.GEmojiCodeGenerators
{
    using System;
    using System.Net;
    using System.Text;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.IO;

    public class Program
    {
        public static void Main()
        {
            using (var fs = File.Create("emoji.txt"))
            using (var sw = new StreamWriter(fs))
            {
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
                        sw.WriteLine($"[\"{aliase.ToString()}\"] = \"{emoji}\",");
                    }
                }
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