// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TripleCrownValidation
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
            string docsetPath,
            )
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var opt = new CommandLineOptions();
            var needUpdateManifest = false;

            try
            {
                if (opt.Parse(args))
                {
                    //OPSLogger.LogToConsole($"[{PluginName}] OPT args :\n{0}", JsonConvert.SerializeObject(opt, Formatting.Indented));

                    if (!string.IsNullOrEmpty(opt.RepoRootPath))
                    {
                        ////OPSLogger.PathTrimPrefix = opt.RepoRootPath;
                    }

                    needUpdateManifest = ValidateHierarchy(opt).Result || !IsDefaultLocale(opt.Locale);
                }
            }
            catch (Exception ex)
            {
                //OPSLogger.LogSystemError(Microsoft.OpenPublishing.PluginHelper.LogCode.TripleCrown_InternalError, ex.ToString());
            }
            finally
            {
                if (needUpdateManifest)
                {
                    //UpdateManifestFile(opt.OriginalManifestPath, OPSLogger.LogItems, opt);
                }

                //OPSLogger.Flush(opt.LogFilePath, true, true);
            }
        }

        private static async Task<bool> ValidateHierarchy(CommandLineOptions opt)
        {
            //OPSLogger.LogSystemInfo($"[{PluginName}] start to do local validation.");

            var validator = new Validator(opt);
            var (isValid, hierarchyItems) = validator.Validate();

            //OPSLogger.LogSystemInfo($"[{PluginName}] finished to do local validation.");

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

            //OPSLogger.LogSystemInfo($"[{PluginName}] start to update dependency map.");

            // Update DependencyType & Remove Fragments
            var dpProcessor = new DependencyMapProcessor(opt.DependencyFilePath, hierarchyItems, opt.DocsetFolder);
            dpProcessor.UpdateDependencyMap();

            //OPSLogger.LogSystemInfo($"[{PluginName}] finished to update dependency map.");

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
                //OPSLogger.LogUserError(Microsoft.OpenPublishing.PluginHelper.LogCode.TripleCrown_DrySyncError, result.Message);
            }

            return result.IsValid;
        }

        private static bool ValidateHierarchyInOtherLocales(
            bool isValid,
            List<IValidateModel> hierarchyItems,
            CommandLineOptions opt)
        {
            // Check loc token exist
            //OPSLogger.LogSystemInfo($"[{PluginName}] start to check if token existed.");

            var fallbackFolders = opt.FallbackFolders?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ff => Path.Combine(opt.RepoRootPath, ff).BackSlashToForwardSlash())
                .ToList();
            var tokenValidator = new TokenValidator(opt.DependencyFilePath, hierarchyItems, opt.DocsetFolder, fallbackFolders);
            isValid = isValid && tokenValidator.Validate();

            //OPSLogger.LogSystemInfo($"[{PluginName}] finished to check if token existed.");

            // Partial publish
            if (opt.ContinueWithError)
            {
                //OPSLogger.LogSystemInfo("[ContinueWithError]TripleCrown mark invalid module/learningpath begin.");

                PartialPublishProcessor partialPublishProcessor = new PartialPublishProcessor(hierarchyItems, opt);
                partialPublishProcessor.MarkInvalidHierarchyItem();

                //OPSLogger.LogSystemInfo("[ContinueWithError]TripleCrown mark invalid module/learningpath finish.");

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
                //OPSLogger.LogSystemInfo($"[{PluginName}] exception occurs during dry sync step: {ex}");
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

        private static void UpdateManifestFile(string originalManifestPath, List<LogItem> logItems, CommandLineOptions opt)
        {
            try
            {
                foreach (var logItem in logItems.Where(l => l.File != null))
                {
                    logItem.File = ValidationHelper.GetLogItemFilePath(opt.DocsetFolder, opt.RepoRootPath, logItem.File);
                }

                bool isUpdated = false;

                var errorLogItems = logItems.Where(l => l.File != null && l.MessageSeverity == MessageSeverity.Error).ToList();
                var originalPaths = new HashSet<string>(errorLogItems.Select(l => l.File).Distinct());
                var originalManifest = JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(originalManifestPath));
                var newLogCodes = new string[] { LogCode };
                foreach (var manifestFile in originalManifest.Files)
                {
                    if (originalPaths.Contains(manifestFile.Original))
                    {
                        isUpdated = true;
                        manifestFile.LogCodes = manifestFile.LogCodes == null ? newLogCodes : manifestFile.LogCodes.Concat(newLogCodes).ToArray();
                    }
                }

                if (File.Exists(HierarchyGenerator.GetHierarchyFullFileName(originalManifestPath)))
                {
                    /** NOTE! 
                        Although Microsoft.OpenPublishing.Build.DataContracts.ItemToPublish.Metadata's type is Dictionary<string, object>, you can't set complex type in it.
                        If you set a complex type directly in the Metadata, the build will always fail.
                        DHS only support following types, for complex type, please convert it to string before setting the value in Metadata
                        SupportedMetadataTypes = new List<Type>
                        {
                            typeof(string),
                            typeof(string[]),
                            typeof(bool),
                            typeof(int),
                            typeof(long),
                            typeof(double),
                            typeof(DateTime)
                        }.AsReadOnly();
                    **/
                    //var newItemsToPublish = new[]{new Microsoft.OpenPublishing.Build.DataContracts.ItemToPublish
                    //{
                    //    RelativePath = HierarchyGenerator.HierarchyFileName,
                    //    Type = Microsoft.OpenPublishing.Build.DataContracts.PublishItemType.Unknown,
                    //    Metadata = new Dictionary<string, object>{ { "is_hidden", true } }
                    //}};

                    if (originalManifest.ItemsToPublish == null)
                    {
                        originalManifest.ItemsToPublish = newItemsToPublish;
                        isUpdated = true;
                    }
                    else
                    {
                        if (!originalManifest.ItemsToPublish.Any(i => i.RelativePath == HierarchyGenerator.HierarchyFileName))
                        {
                            isUpdated = true;
                            originalManifest.ItemsToPublish = originalManifest.ItemsToPublish.Concat(newItemsToPublish).ToArray();
                        }
                    }
                }

                if (isUpdated)
                {
                    File.WriteAllText(originalManifestPath, JsonConvert.SerializeObject(originalManifest));
                }
            }
            catch (Exception ex)
            {
                //OPSLogger.LogSystemError(Microsoft.OpenPublishing.PluginHelper.LogCode.TripleCrown_ManifestFile_UpdateFailed, LogMessageUtility.FormatMessage(Microsoft.OpenPublishing.PluginHelper.LogCode.TripleCrown_ManifestFile_UpdateFailed, ex.ToString()));
            }
        }
    }
}
