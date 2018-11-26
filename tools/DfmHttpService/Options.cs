// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System;

    using CommandLine;

    public class Options
    {

        [Option(
            'w',
            "workspacePath",
            HelpText = "root path of workspace",
            MetaValue = nameof(String),
            Required = true)]
        public string WorkspacePath { get; set; }

        [Option(
            'p',
            "port",
            HelpText = "dfm http service port",
            MetaValue = nameof(String),
            Required = false)]
        public string Port { get; set; }

        [Option(
            "isDfmLatest",
            HelpText = "use dfm-latest engine(defalut: LegacyMode)",
            MetaValue = nameof(Boolean))]
        public bool IsDfmLatest { get; set; }
    }
}
