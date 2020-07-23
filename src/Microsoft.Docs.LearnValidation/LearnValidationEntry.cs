// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var needUpdateManifest = false;
            LearnValidationLogger.WriteLog = writeLog;
            var config = new LearnValidationConfig(
                repoUrl: repoUrl,
                repoBranch: repoBranch,
                docsetName: docsetName,
                docsetPath: docsetPath,
                publishFilePath: publishFilePath,
                dependencyFilePath: dependencyFilePath,
                manifestFilePath: manifestFilePath,
                environment: environment,
                fallbackDocsetPath: fallbackDocsetPath,
                isLocalizationBuild: isLocalizationBuild);

            try
            {
                Console.WriteLine($"[{PluginName}] config:\n{0}", JsonConvert.SerializeObject(
                new { repoUrl, repoBranch, docsetName, docsetPath, publishFilePath, dependencyFilePath, manifestFilePath, isLocalizationBuild, environment, fallbackDocsetPath },
                Formatting.Indented));

                needUpdateManifest = ValidateHierarchy(config).Result || isLocalizationBuild;
            }
            catch (Exception ex)
            {
                LearnValidationLogger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_InternalError, ex.ToString());
            }
            finally
            {
                if (needUpdateManifest)
                {
                    UpdatePublishFile("", LearnValidationLogger.LogItems.ToList());
                }
            }
        }

        private static async Task<bool> ValidateHierarchy(LearnValidationConfig config)
        {
            var sw = Stopwatch.StartNew();
            Console.WriteLine($"[{PluginName}] start to do local validation.");

            var learnValidationHelper = new LearnValidationHelper(GetLearnValidationEndpoint(), config.RepoBranch);
            var validator = new Validator(learnValidationHelper, manifestFilePath: config.ManifestFilePath);
            var (isValid, hierarchyItems) = validator.Validate();

            Console.WriteLine($"[{PluginName}] local validation done in {sw.ElapsedMilliseconds/1000}s");

            if (!config.IsLocalizationBuild)
            {
                return await ValidateHierarchyInDefaultLocale(isValid, hierarchyItems, config);
            }
            else
            {
                return ValidateHierarchyInOtherLocales(isValid, hierarchyItems, config, learnValidationHelper);
            }
        }

        private static async Task<bool> ValidateHierarchyInDefaultLocale(
            bool isValid,
            List<IValidateModel> hierarchyItems,
            LearnValidationConfig config)
        {
            if (!isValid)
            {
                return false;
            }

            Console.WriteLine($"[{PluginName}] start to update dependency map.");

            // Update DependencyType & Remove Fragments
            var dpProcessor = new DependencyMapProcessor(config.DependencyFilePath, hierarchyItems, config.DocsetPath);
            dpProcessor.UpdateDependencyMap();

            Console.WriteLine($"[{PluginName}] finished to update dependency map.");

            var hierarchy = HierarchyGenerator.GenerateHierarchy(hierarchyItems, config.ManifestFilePath);
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
                LearnValidationLogger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_DrySyncError, result.Message);
            }

            return result.IsValid;
        }

        private static bool ValidateHierarchyInOtherLocales(
            bool isValid,
            List<IValidateModel> hierarchyItems,
            LearnValidationConfig config,
            LearnValidationHelper learnValidationHelper)
        {
            // Check loc token exist
            Console.WriteLine($"[{PluginName}] start to check if token existed.");

            var tokenValidator = new TokenValidator(config.DependencyFilePath, hierarchyItems, config.DocsetPath, config.FallbackDocsetPath);
            isValid = isValid && tokenValidator.Validate();

            Console.WriteLine($"[{PluginName}] finished to check if token existed.");

            // Partial publish
            if (config.IsLocalizationBuild)
            {
                Console.WriteLine("[ContinueWithError]LearnValidation mark invalid module/learningpath begin.");

                PartialPublishProcessor partialPublishProcessor = new PartialPublishProcessor(hierarchyItems, config.DocsetPath, learnValidationHelper);
                partialPublishProcessor.MarkInvalidHierarchyItem();

                Console.WriteLine("[ContinueWithError]LearnValidation mark invalid module/learningpath finish.");

                HierarchyGenerator.GenerateHierarchy(hierarchyItems, config.ManifestFilePath);
            }
            else if (isValid)
            {
                HierarchyGenerator.GenerateHierarchy(hierarchyItems, config.ManifestFilePath);
            }

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
                Console.WriteLine($"[{PluginName}] dry-sync done in {sw.ElapsedMilliseconds/1000}s");

                return results.First(r => string.Equals(r.Locale, Constants.DefaultLocale));
            }
        }

        private static void UpdatePublishFile(string publishFilePath, List<LearnLogItem> logItems)
        {
            // TODO:
            // 1. update has_error property
            // 2. publish hierarchy.json
            LearnValidationLogger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Unimplemented, message: "LearnValidation update publish file not implemented!");
        }

        private static string GetDrySyncEndpoint() => Environment.GetEnvironmentVariable("DOCS_LEARN_DRY_SYNC_ENDPOINT");

        private static string GetLearnValidationEndpoint() => Environment.GetEnvironmentVariable("DOCS_LEARN_VALIDATION_ENDPOINT");
    }
}
