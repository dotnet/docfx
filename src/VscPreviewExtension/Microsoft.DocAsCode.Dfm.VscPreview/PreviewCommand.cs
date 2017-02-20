// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.VscPreview
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Newtonsoft.Json;

    public class PreviewCommand
    {
        public static PreviewJsonConfig ParsePreviewCommand(string baseDir)
        {
            string configFilePath = Path.Combine(baseDir, PreviewConstants.ConfigFile);
            PreviewJsonConfig config;
            try
            {
                if (File.Exists(configFilePath))
                {
                    config = JsonUtility.Deserialize<PreviewJsonConfig>(configFilePath);
                }
                else
                {
                    config = null;
                }
            }
            catch (JsonException e)
            {
                // TODO: reply to extension with an error message
                throw e;
            }
            return MergeDefaultConfig(config);
        }

        private static PreviewJsonConfig MergeDefaultConfig(PreviewJsonConfig config)
        {
            if (config == null)
            {
                config = new PreviewJsonConfig();
                config.MarkupResultLocation = PreviewConstants.MarkupResultLocation;
                config.Port = PreviewConstants.Port;
                config.OutputFolder = PreviewConstants.OutPutFolder;
                config.References = new Dictionary<string, string>(PreviewConstants.References);
                return config;
            }

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
            return config;
        }
    }
}
