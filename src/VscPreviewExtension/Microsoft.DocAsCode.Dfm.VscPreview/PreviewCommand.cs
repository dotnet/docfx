// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.VscPreview
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Common;

    public class PreviewCommand
    {
        public static PreviewJsonConfig ParsePreviewCommand(string baseDir)
        {
            string configFilePath = Path.Combine(baseDir, PreviewConstants.ConfigFile);
            PreviewJsonConfig config = new PreviewJsonConfig();
            if (!string.IsNullOrEmpty(PreviewConstants.ConfigFile) && File.Exists(configFilePath))
            {
                config = JsonUtility.Deserialize<PreviewJsonConfig>(configFilePath);
            }
            return MergeDefaultConfig(config);
        }

        private static PreviewJsonConfig MergeDefaultConfig(PreviewJsonConfig config)
        {
            if (string.IsNullOrEmpty(config.MarkupResultLocation))
            {
                config.MarkupResultLocation = PreviewConstants.MarkupResultLocation;
            }

            if (string.IsNullOrEmpty(config.Port))
            {
                config.Port = PreviewConstants.Port;
            }

            if (string.IsNullOrEmpty(config.OutputFolder))
            {
                config.OutputFolder = PreviewConstants.OutPutFolder;
            }

            if (config.References == null)
            {
                config.References = new Dictionary<string, string>()
                {
                    {"link", "href"},
                    {"script", "src"},
                    {"img", "src"}
                };
            }
            else
            {
                if (!config.References.ContainsKey("link"))
                {
                    config.References["link"] = "href";
                }
                if (!config.References.ContainsKey("script"))
                {
                    config.References["script"] = "src";
                }
                if (!config.References.ContainsKey("img"))
                {
                    config.References["img"] = "src";
                }
            }
            return config;
        }
    }
}
