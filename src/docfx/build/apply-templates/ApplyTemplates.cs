// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class ApplyTemplates
    {
        public static bool Run(ErrorBuilder errors, CommandLineOptions options)
        {
            if (string.IsNullOrEmpty(options.Template))
            {
                throw new InvalidOperationException("Must provide the path to template repo when apply templates.");
            }

            var inputDir = options.WorkingDirectory;
            var outputDir = options.Output ?? Path.Combine(AppContext.BaseDirectory, "outputs");
            var package = new LocalPackage(options.Template);
            var locale = options.Locale ?? "en-us";
            var trustedObj = JToken.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data/docs/trusted-domains.json")));
            var trustedDomains = JsonUtility.ToObject<Dictionary<string, TrustedDomains>>(errors, trustedObj);
            var config = new Config
            {
                TrustedDomains = trustedDomains,
                DryRun = false,
                Template = new PackagePath(options.Template),
            };

            var fileResolver = new FileResolver(package);
            var jsonSchemaLoader = new JsonSchemaLoader(fileResolver);
            var jsonSchemaProvider = new JsonSchemaProvider(config, package, jsonSchemaLoader);
            var templateEngine = TemplateEngine.CreateTemplateEngine(errors, config, null, locale, package, fullBuild: false);

            Parallel.ForEach(
                Directory.GetFiles(inputDir, "*.*", SearchOption.AllDirectories),
                filePath =>
                {
                    ApplyTemplatesCore(errors, filePath, inputDir, outputDir, jsonSchemaProvider, templateEngine);
                });

            return errors.HasError;
        }

        private static void ApplyTemplatesCore(
            ErrorBuilder errors,
            string filePath,
            string inputDir,
            string outputDir,
            JsonSchemaProvider jsonSchemaProvider,
            TemplateEngine templateEngine)
        {
            var file = new FilePath(filePath);
            var pageModel = JsonUtility.Deserialize<JObject>(errors, File.ReadAllText(file.Path), file);
            var schema = pageModel.GetValue("schema")?.ToString();

            if (schema is not null && schema.Equals("toc", StringComparison.Ordinal))
            {
                var model = templateEngine.RunJavaScript("toc.json.js", JsonUtility.ToJObject(pageModel));
                File.WriteAllText(GetOutPathWithDifferentExtension(inputDir, filePath, outputDir, "json"), JsonUtility.Serialize(model));
            }
            else
            {
                var isContentRenderType = jsonSchemaProvider.GetRenderType(ContentType.Page, new SourceInfo<string?>(schema)) == RenderType.Content;
                if (isContentRenderType)
                {
                    var (model, _) = templateEngine.CreateTemplateModel(file, schema is null ? string.Empty : schema.ToString(), pageModel);

                    File.WriteAllText(GetOutPathWithDifferentExtension(inputDir, filePath, outputDir), JsonUtility.Serialize(model));
                }
                else
                {
                    var model = templateEngine.RunJavaScript($"{schema}.json.js", pageModel);
                    File.WriteAllText(GetOutPathWithDifferentExtension(inputDir, filePath, outputDir), JsonUtility.Serialize(model));
                }
            }
        }

        private static string GetOutPathWithDifferentExtension(string inputDir, string filePath, string outputDir, string extension = "raw.page.json")
        {
            var relativeFilePath = Path.GetRelativePath(inputDir, filePath);
            var outputPath = Path.Combine(outputDir, relativeFilePath);
            return Path.ChangeExtension(outputPath, extension);
        }
    }
}
