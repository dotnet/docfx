// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Common;

    public class BuildInfo
    {
        public const string FileName = "build.info";

        /// <summary>
        /// The directory name from base dir.
        /// </summary>
        public string DirectoryName { get; set; }
        /// <summary>
        /// The build start time for this build.
        /// </summary>
        public DateTime BuildStartTime { get; set; }
        /// <summary>
        /// The version of docfx.
        /// </summary>
        public string DocfxVersion { get; set; }
        /// <summary>
        /// The hash info for all plugins.
        /// </summary>
        public string PluginHash { get; set; }
        /// <summary>
        /// The hash info for templates.
        /// </summary>
        public string TemplateHash { get; set; }
        /// <summary>
        /// The SHA of the current commit from.
        /// </summary>
        public string CommitFromSHA { get; set; }
        /// <summary>
        /// The SHA of the current commit to.
        /// </summary>
        public string CommitToSHA { get; set; }
        /// <summary>
        /// The file info for each version.
        /// </summary>
        public List<BuildVersionInfo> Versions { get; } = new List<BuildVersionInfo>();
        /// <summary>
        /// The post process information
        /// </summary>
        public PostProcessInfo PostProcessInfo { get; set; }
        /// <summary>
        /// Is this cache valid.
        /// </summary>
        public bool IsValid { get; set; } = true;
        /// <summary>
        /// Details about why cache is not valid.
        /// </summary>
        public string Message { get; set; }

        public static BuildInfo Load(string baseDir)
        {
            return Load(baseDir, false);
        }

        public static BuildInfo Load(string baseDir, bool onlyValid)
        {
            if (baseDir == null)
            {
                return null;
            }

            var expanded = Environment.ExpandEnvironmentVariables(baseDir);
            if (expanded.Length == 0)
            {
                expanded = ".";
            }
            baseDir = Path.GetFullPath(expanded);
            if (!File.Exists(Path.Combine(baseDir, FileName)))
            {
                Logger.LogInfo($"Cannot load build info: '{FileName}' not found under '{baseDir}'");
                return null;
            }

            BuildInfo buildInfo;
            try
            {
                buildInfo = JsonUtility.Deserialize<BuildInfo>(Path.Combine(baseDir, FileName));
                if (onlyValid && !buildInfo.IsValid)
                {
                    Logger.LogInfo($"Cannot load build info as cache is invalid: {buildInfo.Message}");
                    return null;
                }

                var targetDirectory = Path.Combine(baseDir, buildInfo.DirectoryName);
                foreach (var version in buildInfo.Versions)
                {
                    version.BaseDir = targetDirectory;
                    version.Load(targetDirectory);
                }
                buildInfo.PostProcessInfo?.Load(targetDirectory);
            }
            catch (Exception ex)
            {
                Logger.LogInfo($"Exception occurs when loading build info from '{Path.Combine(baseDir, FileName)}', message: {ex.Message}.");
                return null;
            }
            return buildInfo;
        }

        public void SaveVersionsManifet(string baseDir)
        {
            var expanded = Path.GetFullPath(Environment.ExpandEnvironmentVariables(baseDir));
            foreach (var version in Versions)
            {
                version.SaveManifest();
            }
        }

        public void Save(string baseDir)
        {
            var expanded = Path.GetFullPath(Environment.ExpandEnvironmentVariables(baseDir));
            var targetDirectory = Path.Combine(expanded, DirectoryName);
            foreach (var version in Versions)
            {
                version.Save(targetDirectory);
            }
            PostProcessInfo?.Save(targetDirectory);
            JsonUtility.Serialize(Path.Combine(expanded, FileName), this);
        }
    }
}
