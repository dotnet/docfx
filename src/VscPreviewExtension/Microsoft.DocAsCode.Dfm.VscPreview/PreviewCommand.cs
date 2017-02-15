// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.VscPreview
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Common;

    public class PreviewCommand
    {
        public static PreviewJsonConfig ParsePreviewCommand(string basedir)
        {
            string configFilePath = Path.Combine(basedir, PreviewConstants.ConfigFile);
            PreviewJsonConfig config = new PreviewJsonConfig();
            if (!string.IsNullOrEmpty(PreviewConstants.ConfigFile) && File.Exists(configFilePath))
                config = JsonUtility.Deserialize<PreviewJsonConfig>(configFilePath);
            return MergeDefaultConfig(config);
        }

        private static PreviewJsonConfig MergeDefaultConfig(PreviewJsonConfig config)
        {
            if (string.IsNullOrEmpty(config.MarkupResultLocation))
                config.MarkupResultLocation = PreviewConstants.MarkUpResultLocation;

            if (string.IsNullOrEmpty(config.Port))
                config.Port = PreviewConstants.Port;

            if (string.IsNullOrEmpty(config.OutputFolder))
                config.OutputFolder = PreviewConstants.OutPutFolder;

            if (config.Reference == null)
                config.Reference = new Dictionary<string, string>()
                {
                    {"link", "href"},
                    {"script", "src"},
                    {"img", "src"}
                };
            else
            {
                if (!config.Reference.ContainsKey("link"))
                    config.Reference["link"] = "href";
                if (!config.Reference.ContainsKey("script"))
                    config.Reference["script"] = "src";
                if (!config.Reference.ContainsKey("img"))
                    config.Reference["img"] = "src";
            }
            return config;
        }
    }
}
