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

    internal sealed class MetadataCommand : ISubCommand
    {
        public MetadataJsonConfig Config { get; }

        public MetadataCommand(MetadataCommandOptions options)
        {
            // TODO: Read config from project list
            throw new NotImplementedException();
        }

        public void Exec(SubCommandRunningContext context)
        {
            throw new NotImplementedException();
        }

        private class MetadataConfig
        {
            [JsonProperty("metadata")]
            public MetadataJsonConfig Item { get; set; }
        }
    }
}
