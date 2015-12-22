// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.EntityModel.Builders;
    using Microsoft.DocAsCode.Plugins;
    using Newtonsoft.Json;

    internal sealed class BuildCommand : ISubCommand
    {
        public BuildJsonConfig Config { get; }

        public BuildCommand(BuildCommandOptions options)
        {
            if (string.IsNullOrEmpty(options.ConfigFile))
            {
                options.ConfigFile = DocAsCode.Constants.ConfigFileName;
            }
            if (!File.Exists(options.ConfigFile)) throw new FileNotFoundException($"Config file {options.ConfigFile} does not exist!");

            Config = JsonUtility.Deserialize<BuildConfig>(options.ConfigFile).Item;
            // TODO: Expand file mapping
            // merge config
        }

        public void Exec(SubCommandRunningContext context)
        {
            throw new NotImplementedException();
        }

        private class BuildConfig
        {
            [JsonProperty("build")]
            public BuildJsonConfig Item { get; set; }
        }
    }
}
