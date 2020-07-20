// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Docs.LearnValidation
{
    public static class TripleCrownValidation
    {
        private const string LogCode = "InvalidHierarchyItem";
        private const string PluginName = "TripleCrownPlugin";

        static void Run(
            string repoUrl,
            string repoBranch,
            string docsetName,
            string docsetPath,
            string publishFilePath,
            string dependencyFilePath,
            string manifestFilePath,
            string environment,
            bool isLocalizationBuild,
            string fallbackDocsetPath = null
            )
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var needUpdateManifest = false;
            var opt = new CommandLineOptions();

            try
            {

                Console.WriteLine($"[{PluginName}] OPT args :\n{0}", JsonConvert.SerializeObject(
                    new { repoUrl, repoBranch, docsetName, docsetPath, publishFilePath, dependencyFilePath, manifestFilePath, isLocalizationBuild, environment, fallbackDocsetPath},
                    Formatting.Indented));

                    needUpdateManifest = ValidateHierarchy(opt).Result || !IsDefaultLocale(opt.Locale);
            }
            catch (Exception ex)
            {
                Logger.Log(ErrorLevel.Error, ErrorCode.TripleCrown_InternalError, ex.ToString());
            }
            finally
            {
                if (needUpdateManifest)
                {
                    UpdatePublishFile("", Logger.LogItems.ToList());
                }
            }
        }

        private static async Task<bool> ValidateHierarchy(CommandLineOptions opt)
        {
            Console.WriteLine($"[{PluginName}] start to do local validation.");

            var validator = new Validator(opt);
            var (isValid, hierarchyItems) = validator.Validate();

            Console.WriteLine($"[{PluginName}] finished to do local validation.");

            if (IsDefaultLocale(opt.Locale))
            {
                return await ValidateHierarchyInDefaultLocale(isValid, hierarchyItems, opt);
            }
            else
            {
                return ValidateHierarchyInOtherLocales(isValid, hierarchyItems, opt);
            }
        }

        private static async Task<bool> ValidateHierarchyInDefaultLocale(
            bool isValid,
            List<IValidateModel> hierarchyItems,
            CommandLineOptions opt)
        {
            if (!isValid)
            {
                return false;
            }

            Console.WriteLine($"[{PluginName}] start to update dependency map.");

            // Update DependencyType & Remove Fragments
            var dpProcessor = new DependencyMapProcessor(opt.DependencyFilePath, hierarchyItems, opt.DocsetFolder);
            dpProcessor.UpdateDependencyMap();

            Console.WriteLine($"[{PluginName}] finished to update dependency map.");

            var hierarchy = HierarchyGenerator.GenerateHierarchy(hierarchyItems, opt.OriginalManifestPath);
            var repoUrl = Utility.TransformGitUrl(opt.RepoUrl);
            var result = await TryDrySync(
                opt.Branch,
                Constants.DefaultLocale,
                opt.DocsetName,
                repoUrl,
                hierarchy,
                opt.DrySyncEndpoint);

            if (!result.IsValid)
            {
                Logger.Log(ErrorLevel.Error, ErrorCode.TripleCrown_DrySyncError, result.Message);
            }

            return result.IsValid;
        }

        private static bool ValidateHierarchyInOtherLocales(
            bool isValid,
            List<IValidateModel> hierarchyItems,
            CommandLineOptions opt)
        {
            // Check loc token exist
            Console.WriteLine($"[{PluginName}] start to check if token existed.");

            // TODO: pass fallback path
            var tokenValidator = new TokenValidator(opt.DependencyFilePath, hierarchyItems, opt.DocsetFolder, "");
            isValid = isValid && tokenValidator.Validate();

            Console.WriteLine($"[{PluginName}] finished to check if token existed.");

            // Partial publish
            if (opt.ContinueWithError)
            {
                Console.WriteLine("[ContinueWithError]TripleCrown mark invalid module/learningpath begin.");

                PartialPublishProcessor partialPublishProcessor = new PartialPublishProcessor(hierarchyItems, opt);
                partialPublishProcessor.MarkInvalidHierarchyItem();

                Console.WriteLine("[ContinueWithError]TripleCrown mark invalid module/learningpath finish.");

                HierarchyGenerator.GenerateHierarchy(hierarchyItems, opt.OriginalManifestPath);
            }
            else if (isValid)
            {
                HierarchyGenerator.GenerateHierarchy(hierarchyItems, opt.OriginalManifestPath);
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
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var data = await response.Content.ReadAsStringAsync();
                var results = JsonConvert.DeserializeObject<List<ValidationResult>>(data);

                return results.First(r => string.Equals(r.Locale, Constants.DefaultLocale));
            }
        }

        private static bool IsDefaultLocale(string locale)
            => string.Equals(locale, Constants.DefaultLocale, StringComparison.OrdinalIgnoreCase);

        private static void UpdatePublishFile(string publishFilePath, List<LogItem> logItems)
        {
            // TODO:
            // 1. update has_error property
            // 2. publish hierarchy.json
            throw new NotImplementedException();
        }
    }
}
