using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Docs.Build;

namespace ImportTemplateResource
{
    class Program
    {
        private static readonly string[] s_defaultLocales = new[] {
            "ar-sa", "bg-bg", "ca-es", "cs-cz", "da-dk", "de-at", "de-ch", "de-de", "el-gr", "en-au", "en-ca", "en-gb",
            "en-us", "en-ie", "en-in", "en-my", "en-nz", "en-sg", "en-za", "es-es", "es-mx", "et-ee", "eu-es", "fi-fi", "fr-be", "fr-ca", "fr-ch",
            "fr-fr", "ga-ie", "gl-es", "he-il", "hi-in", "hr-hr", "hu-hu", "id-id", "is-is", "it-ch", "it-it", "ja-jp", "kk-kz", "ko-kr", "lb-lu",
            "lt-lt", "lv-lv", "mt-mt", "ms-my", "nb-no", "nl-be", "nl-nl", "pl-pl", "pt-br", "pt-pt", "ro-ro", "ru-ru", "sk-sk", "sl-si", "sr-cyrl-rs",
            "sr-latn-rs", "sv-se", "th-th", "tr-tr", "uk-ua", "vi-vn", "zh-cn", "zh-hk", "zh-tw"};

        static async Task<int> Main(string[] args)
        {
            var sourceTemplateRepoUrl = args.Length > 0 ? args[0] : throw new ArgumentNullException("source template repo url");
            var gitToken = args.Length > 1 ? args[1] : throw new ArgumentNullException("git access token");
            var locales = args.Length > 2 ? args[2].Split(",") : s_defaultLocales;
            var templateResourceFolder = "src/Microsoft.Docs.Template/resources";
            Directory.CreateDirectory(templateResourceFolder);

            await ParallelUtility.ForEach(locales, async locale =>
            {
                var templateRepoUrl = sourceTemplateRepoUrl;
                if (!string.Equals("en-us", locale, StringComparison.OrdinalIgnoreCase))
                {
                    templateRepoUrl = templateRepoUrl + $".{locale}";
                }

                var uri = new Uri(templateRepoUrl);
                var cloneDir = PathUtility.NormalizeFolder(Path.Combine(AppData.AppDataDir, "tool", "_theme", Path.Combine(uri.Host, uri.AbsolutePath.Substring(1))));
                Directory.CreateDirectory(cloneDir);

                if (!GitUtility.IsRepo(cloneDir))
                {
                    await ProcessUtility.Execute("git", $"-c http.{uri.Host}.extraheader=\"basic {gitToken}\" clone {templateRepoUrl} {cloneDir} --branch master --single-branch", null, false, false);
                }
                else
                {
                    await ProcessUtility.Execute("git", "reset --hard", cloneDir, false, false);
                    await ProcessUtility.Execute("git", $"-c http.{uri.Host}.extraheader=\"basic {gitToken}\" fetch ", cloneDir, false, false);
                }

                MoveResourceFiles(cloneDir, locale);
            });

            return 0;

            void MoveResourceFiles(string cloneDir, string locale)
            {
                var sourceTokenFile = Path.Combine(cloneDir, $"LocalizedTokens/docs({locale}).html", "tokens.json");
                var targetTokenFile = Path.Combine(templateResourceFolder, $"tokens.{locale}.resx");
                if (!File.Exists(sourceTokenFile))
                {
                    lock (Console.Out)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Can not found template resource file: {sourceTokenFile} for locale: {locale}");
                        Console.ResetColor();
                    }

                    return;
                }

                var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                XDocument doc = XDocument.Load(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "tokens.resx"));
                foreach(var (key, value) in JsonUtility.Deserialize<Dictionary<string, string>>(File.ReadAllText(sourceTokenFile)).model)
                {
                    if (keys.Add(key))
                    {
                        var name = new XElement("data", new XAttribute("name", key));
                        name.Add(new XElement("value", value));
                        doc.Root.Add(name);
                    }
                }

                doc.Save(targetTokenFile, SaveOptions.None);
            }
        }
    }
}
