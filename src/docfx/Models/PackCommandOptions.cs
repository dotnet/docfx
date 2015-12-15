// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using CommandLine;

    internal class PackCommandOptions
    {
        [Option('u', "url", HelpText = "The base url of yaml file.", Required = true)]
        public string BaseUrl { get; set; }

        [Option('s', "source", HelpText = "The base folder for yaml files.", Required = true)]
        public string Source { get; set; }

        [Option('g', "glob", HelpText = "The glob partten for yaml files.", Required = true)]
        public string Glob { get; set; }

        [Option('n', "name", HelpText = "The name of package.")]
        public string Name { get; set; }

        [Option('a', "append", HelpText = "Append the package.")]
        public bool AppendMode { get; set; }

        [Option('f', "flat", HelpText = "Flat href path.")]
        public bool FlatMode { get; set; }

        [Option('o', "output")]
        public string OutputFolder { get; set; }
    }
}
