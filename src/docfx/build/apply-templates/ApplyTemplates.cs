// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class ApplyTemplates
    {
        public static bool Run(ErrorBuilder errors, CommandLineOptions options)
        {
            if (string.IsNullOrEmpty(options.Template))
            {
                throw new InvalidOperationException("Must provide the path of template repo when apply templates.");
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
            var templateEngine = TemplateEngine.CreateTemplateEngine(errors, config, locale, package);

            Directory.CreateDirectory(outputDir);

            using var scope = Progress.Start("Apply templates...");
            ParallelUtility.ForEach(
                scope,
                errors,
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
                var outputPath = GetOutPathWithDifferentExtension(inputDir, filePath, outputDir, "json");
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
                File.WriteAllText(outputPath, JsonUtility.Serialize(model));
            }
            else
            {
                var isContentRenderType = jsonSchemaProvider.IsContentRenderType(schema);
                if (isContentRenderType)
                {
                    var (model, metadata) = templateEngine.CreateTemplateModel(file, schema is null ? string.Empty : schema.ToString(), pageModel);
                    var outputPath = GetOutPathWithDifferentExtension(inputDir, filePath, outputDir);
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
                    File.WriteAllText(outputPath, JsonUtility.Serialize(model));
                    File.WriteAllText(GetOutPathWithDifferentExtension(inputDir, filePath, outputDir, "mta.json"), JsonUtility.Serialize(metadata));
                }
                else
                {
                    var model = templateEngine.RunJavaScript($"{schema}.json.js", pageModel);
                    var outputPath = GetOutPathWithDifferentExtension(inputDir, filePath, outputDir, "json");
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
                    File.WriteAllText(outputPath, JsonUtility.Serialize(model));
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
