// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Docs.LearnValidation.Models;
using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Docs.LearnValidation
{
    public static class LearnValidationEntry
    {
        private const string PluginName = "LearnValidationPlugin";

        public static void Run(
            string repoUrl,
            string repoBranch,
            string docsetName,
            string docsetPath,
            string docsetOutputPath,
            string publishFilePath,
            string dependencyFilePath,
            string manifestFilePath,
            string environment,
            bool isLocalizationBuild,
            Action<LearnLogItem> writeLog,
            string fallbackDocsetPath = null
            )
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var config = new LearnValidationConfig(
                repoUrl: repoUrl,
                repoBranch: repoBranch,
                docsetName: docsetName,
                docsetPath: docsetPath,
                docsetOutputPath: docsetOutputPath,
                publishFilePath: publishFilePath,
                dependencyFilePath: dependencyFilePath,
                manifestFilePath: manifestFilePath,
                environment: environment,
                fallbackDocsetPath: fallbackDocsetPath,
                isLocalizationBuild: isLocalizationBuild);
            var logger = new LearnValidationLogger(writeLog);

            var configStr = JsonConvert.SerializeObject(
                new { repoUrl, repoBranch, docsetName, docsetPath, publishFilePath, dependencyFilePath, manifestFilePath, isLocalizationBuild, environment, fallbackDocsetPath },
                Formatting.Indented);

            Console.WriteLine($"[{PluginName}] config:\n{configStr}");
            ValidateHierarchy(config, logger).GetAwaiter().GetResult();
            
        }

        private static async Task<bool> ValidateHierarchy(LearnValidationConfig config, LearnValidationLogger logger)
        {
            var sw = Stopwatch.StartNew();
            Console.WriteLine($"[{PluginName}] start to do local validation.");

            var learnValidationHelper = new LearnValidationHelper(GetLearnValidationEndpoint(), config.RepoBranch);
            var validator = new Validator(learnValidationHelper, manifestFilePath: config.ManifestFilePath, logger);
            var (isValid, hierarchyItems) = validator.Validate();

            Console.WriteLine($"[{PluginName}] local validation done in {sw.ElapsedMilliseconds / 1000}s");

            if (!config.IsLocalizationBuild)
            {
                return await ValidateHierarchyInDefaultLocale(isValid, hierarchyItems, config, logger);
            }
            else
            {
                return ValidateHierarchyInOtherLocales(isValid, hierarchyItems, config, learnValidationHelper, logger);
            }
        }

        private static async Task<bool> ValidateHierarchyInDefaultLocale(
            bool isValid,
            List<IValidateModel> hierarchyItems,
            LearnValidationConfig config,
            LearnValidationLogger logger)
        {
            if (!isValid)
            {
                return false;
            }

            var hierarchy = HierarchyGenerator.GenerateHierarchy(hierarchyItems, config.DocsetOutputPath);
            var repoUrl = Utility.TransformGitUrl(config.RepoUrl);

            var result = await TryDrySync(
                config.RepoBranch,
                Constants.DefaultLocale,
                config.DocsetName,
                repoUrl,
                hierarchy,
                GetDrySyncEndpoint());

            if (!result.IsValid)
            {
                logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_DrySyncError, file: null, result.Message);
            }

            return result.IsValid;
        }

        private static bool ValidateHierarchyInOtherLocales(
            bool isValid,
            List<IValidateModel> hierarchyItems,
            LearnValidationConfig config,
            LearnValidationHelper learnValidationHelper,
            LearnValidationLogger logger)
        {
            var tokenValidator = new TokenValidator(config.DependencyFilePath, hierarchyItems, config.DocsetPath, config.FallbackDocsetPath, logger);
            isValid = isValid && tokenValidator.Validate();
            InvalidFilesProvider partialPublishProcessor = new InvalidFilesProvider(hierarchyItems, learnValidationHelper, logger);
            var filesToDelete = partialPublishProcessor.GetFilesToDelete();
            HierarchyGenerator.GenerateHierarchy(hierarchyItems, config.DocsetOutputPath);
            RemoveInvalidPublishItems(config.PublishFilePath, filesToDelete, logger);

            return isValid;
        }

        private async static Task<ValidationResult> TryDrySync(
            string branch,
            string locale,
            string docsetName,
            string repoUrl,
            RawHierarchy hierarchy,
            string drySyncEndpoint)
        {
            if (string.IsNullOrEmpty(drySyncEndpoint))
            {
                Console.WriteLine($"Skipping dry-sync for unset endpoint");
                return new ValidationResult(branch, locale, true, "Hierarchy dry-sync endpoint not defined");
            }

            try
            {
                return await DrySync(branch, locale, docsetName, repoUrl, hierarchy, drySyncEndpoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{PluginName}] exception occurs during dry sync step: {ex}");
                // regard current hierarchy as valid if any unhandled exceptions occurs to avoid blocking build. 
                return new ValidationResult(branch, locale, true, string.Empty);
            }
        }

        private async static Task<ValidationResult> DrySync(
            string branch,
            string locale,
            string docsetName,
            string repoUrl,
            RawHierarchy hierarchy,
            string drySyncEndpoint)
        {
            var body = JsonConvert.SerializeObject(new DrySyncMessage
            {
                RawHierarchy = hierarchy,
                Locale = locale,
                Branch = branch,
                DocsetName = docsetName,
                RepoUrl = repoUrl
            });
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(drySyncEndpoint),
                Method = HttpMethod.Post,
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            using (var client = new HttpClient())
            {
                Console.WriteLine($"[{PluginName}] start to call dry-sync...");
                var sw = Stopwatch.StartNew();
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var data = await response.Content.ReadAsStringAsync();
                var results = JsonConvert.DeserializeObject<List<ValidationResult>>(data);
                Console.WriteLine($"[{PluginName}] dry-sync done in {sw.ElapsedMilliseconds / 1000}s");

                return results.First(r => string.Equals(r.Locale, Constants.DefaultLocale));
            }
        }

        private static void RemoveInvalidPublishItems(string publishFilePath, HashSet<string> invalidFiles, LearnValidationLogger logger)
        {
            var publishModel = JsonConvert.DeserializeObject<LearnPublishModel>(File.ReadAllText(publishFilePath));
            publishModel.Files.RemoveAll(item => invalidFiles.Contains(item.SourcePath));

            if (logger.HasFileWithError)
            {
                foreach (var item in publishModel.Files)
                {
                    item.HasError = item.HasError || logger.FileHasError(item.SourcePath);
                }
            }
            File.WriteAllText(publishFilePath, JsonConvert.SerializeObject(publishModel));
        }

        private static string GetDrySyncEndpoint() => Environment.GetEnvironmentVariable("DOCS_LEARN_DRY_SYNC_ENDPOINT");

        private static string GetLearnValidationEndpoint() => Environment.GetEnvironmentVariable("DOCS_LEARN_VALIDATION_ENDPOINT");
    }
}
