// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Docs.LearnValidation.Models;
using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;

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
            bool noDrySync,
            Action<LearnLogItem> writeLog,
            ILearnServiceAccessor learnServiceAccessor,
            string fallbackDocsetPath = null)
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
                isLocalizationBuild: isLocalizationBuild,
                noDrySync: noDrySync);
            var logger = new LearnValidationLogger(writeLog);

            var configStr = JsonConvert.SerializeObject(
                new { repoUrl, repoBranch, docsetName, docsetPath, publishFilePath, dependencyFilePath, manifestFilePath, isLocalizationBuild, environment, fallbackDocsetPath },
                Formatting.Indented);

            Console.WriteLine($"[{PluginName}] config:\n{configStr}");
            ValidateHierarchy(config, logger, learnServiceAccessor);
        }

        private static bool ValidateHierarchy(LearnValidationConfig config, LearnValidationLogger logger, ILearnServiceAccessor learnServiceAccessor)
        {
            var sw = Stopwatch.StartNew();
            Console.WriteLine($"[{PluginName}] start to do local validation.");

            var learnValidationHelper = new LearnValidationHelper(config.RepoBranch, learnServiceAccessor);
            var validator = new Validator(manifestFilePath: config.ManifestFilePath, logger);
            var (isValid, hierarchyItems) = validator.Validate();

            Console.WriteLine($"[{PluginName}] local validation done in {sw.ElapsedMilliseconds / 1000}s");

            if (!config.IsLocalizationBuild)
            {
                if (isValid)
                {
                    HierarchyGenerator.GenerateHierarchy(hierarchyItems, config.DocsetOutputPath);
                }
                return true;
            }
            else
            {
                return ValidateHierarchyInOtherLocales(isValid, hierarchyItems, config, learnValidationHelper, logger);
            }
        }

        private static bool ValidateHierarchyInOtherLocales(
            bool isValid,
            List<IValidateModel> hierarchyItems,
            LearnValidationConfig config,
            LearnValidationHelper learnValidationHelper,
            LearnValidationLogger logger)
        {
            var partialPublishProcessor = new InvalidFilesProvider(hierarchyItems, learnValidationHelper, logger);
            var filesToDelete = partialPublishProcessor.GetFilesToDelete();
            HierarchyGenerator.GenerateHierarchy(hierarchyItems, config.DocsetOutputPath);
            RemoveInvalidPublishItems(config.PublishFilePath, filesToDelete, logger);

            return isValid;
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
    }
}
