// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using CommandLine;
    using System.Collections.Generic;

    class MetadataCommandOptions
    {
        [Option('f', "force", HelpText = "Force re-generate all the metadata")]
        public bool ForceRebuild { get; set; }

        [Option('o', "output")]
        public string OutputFolder { get; set; }

        [Option("raw", HelpText = "Preserve the existing xml comment tags inside 'summary' triple slash comments")]
        public bool PreserveRawInlineComments { get; set; }

        [ValueList(typeof(List<string>))]
        public List<string> Projects { get; set; }
    }
}
