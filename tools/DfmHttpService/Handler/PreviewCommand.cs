// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Newtonsoft.Json;

    public class PreviewCommand
    {
        public static PreviewJsonConfig ParsePreviewCommand(string workspacePath)
        {
            string configFilePath = Path.Combine(workspacePath, PreviewConstants.ConfigFilename);
            PreviewJsonConfig config = null;
            try
            {
                if (File.Exists(configFilePath))
                {
                    config = JsonUtility.Deserialize<PreviewJsonConfig>(configFilePath);
                }
            }
            catch (JsonException ex)
            {
                throw new HandlerServerException($"Error happened while parsing config file, {ex.Message}");
            }
            return MergeDefaultConfig(config);
        }

        private static PreviewJsonConfig MergeDefaultConfig(PreviewJsonConfig config)
        {
            if (config == null)
            {
                config = new PreviewJsonConfig();
                config.BuildSourceFolder = PreviewConstants.BuildSourceFolder;
                config.BuildOutputSubFolder = PreviewConstants.BuildOutputSubfolder;
                config.MarkupTagType = PreviewConstants.MarkupTagType;
                config.MarkupClassName = PreviewConstants.MarkupClassName;
                config.OutputFolder = PreviewConstants.OutputFolder;
                config.PageRefreshFunctionName = PreviewConstants.PageRefreshFunctionName;
                config.DocfxServerPort = PreviewConstants.DocfxServerPort;
                config.NavigationPort = PreviewConstants.NavigationPort;
                config.References = new Dictionary<string, string>(PreviewConstants.References);
                config.TocMetadataName = PreviewConstants.TocMetadataName;
                return config;
            }

            if (string.IsNullOrEmpty(config.BuildSourceFolder))
            {
                config.BuildSourceFolder = PreviewConstants.BuildSourceFolder;
            }

            if (string.IsNullOrEmpty(config.BuildOutputSubFolder))
            {
                config.BuildOutputSubFolder = PreviewConstants.BuildOutputSubfolder;
            }

            if (string.IsNullOrEmpty(config.MarkupTagType))
            {
                config.MarkupTagType = PreviewConstants.MarkupTagType;
            }

            if (string.IsNullOrEmpty(config.MarkupClassName))
            {
                config.MarkupClassName = PreviewConstants.MarkupClassName;
            }

            if (string.IsNullOrEmpty(config.OutputFolder))
            {
                config.OutputFolder = PreviewConstants.OutputFolder;
            }

            if (string.IsNullOrEmpty(config.PageRefreshFunctionName))
            {
                config.PageRefreshFunctionName = PreviewConstants.PageRefreshFunctionName;
            }

            if (string.IsNullOrEmpty(config.DocfxServerPort))
            {
                config.DocfxServerPort = PreviewConstants.DocfxServerPort;
            }

            if (string.IsNullOrEmpty(config.NavigationPort))
            {
                config.NavigationPort = PreviewConstants.NavigationPort;
            }

            if (config.References == null)
            {
                config.References = new Dictionary<string, string>(PreviewConstants.References);
            }
            else
            {
                foreach (var reference in PreviewConstants.References)
                {
                    if (!config.References.ContainsKey(reference.Key))
                    {
                        config.References[reference.Key] = reference.Value;
                    }
                }
            }

            if (string.IsNullOrEmpty(config.TocMetadataName))
            {
                config.TocMetadataName = PreviewConstants.TocMetadataName;
            }

            return config;
        }
    }
}
