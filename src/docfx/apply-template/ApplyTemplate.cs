// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class ApplyTemplate
    {
        public static bool Run(CommandLineOptions options)
        {
            using var errors = new ErrorWriter(options.Log);

            if (string.IsNullOrEmpty(options.Template))
            {
                errors.Add(Errors.ApplyTemplate.TemplateNotSpecified());
                return errors.HasError;
            }

            var directory = options.Input ?? ".";

            var (templateEngineRunner, trustedDomains) = Prepare(errors, options.Template, options.Locale ?? "en-us");

            Parallel.ForEach(
                Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories),
                filePath =>
                {
                    ApplyTemplateCore(
                        errors, filePath, directory, options.Output ?? directory, templateEngineRunner, trustedDomains, options.Locale ?? "en-us");
                });

            return errors.HasError;
        }

        private static (TemplateEngineRunner, Dictionary<string, TrustedDomains>) Prepare(ErrorBuilder errors, string templatePath, string locale)
        {
            var package = new LocalPackage(templatePath);
            var global = TemplateEngine.LoadGlobalTokens(errors, package, locale);

            var templateEngineRunner = new TemplateEngineRunner(package, global);
            var trustedObj = JToken.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data/docs/trusted-domains.json")));
            var trustedDomains = JsonUtility.ToObject<Dictionary<string, TrustedDomains>>(errors, trustedObj);

            return (templateEngineRunner, trustedDomains);
        }

        private static void ApplyTemplateCore(
            ErrorBuilder errors,
            string filePath,
            string inputDir,
            string outputDir,
            TemplateEngineRunner templateEngineRunner,
            Dictionary<string, TrustedDomains> trustedDomains,
            string locale)
        {
            var file = new FilePath(filePath);
            var pageModel = JsonUtility.Deserialize<JObject>(errors, File.ReadAllText(file.Path), file);
            var schema = pageModel.GetValue("schema");
            var (model, _) = TemplateModelBuilder.CreateTemplateModel(
                errors,
                templateEngineRunner,
                file,
                pageModel,
                schema is null ? string.Empty : schema.ToString(),
                locale,
                null,
                trustedDomains: trustedDomains);

            File.WriteAllText(GetOutPathWithDifferentExtension(inputDir, filePath, outputDir), JsonUtility.Serialize(model));
        }

        private static string GetOutPathWithDifferentExtension(string inputDir, string filePath, string outputDir)
        {
            var relativeFilePath = Path.GetRelativePath(inputDir, filePath);
            var outputPath = Path.Combine(outputDir, relativeFilePath);
            return Path.ChangeExtension(outputPath, "raw.page.json");
        }
    }
}
