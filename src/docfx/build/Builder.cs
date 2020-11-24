// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class Builder
    {
        private readonly string _workingDirectory;
        private readonly CommandLineOptions _options;

        public Builder(string workingDirectory, CommandLineOptions options)
        {
            _workingDirectory = workingDirectory;
            _options = options;
        }

        public static bool Run(string workingDirectory, CommandLineOptions options)
        {
            return new Builder(workingDirectory, options).Build();
        }

        public bool Build()
        {
            var stopwatch = Stopwatch.StartNew();
            using var errors = new ErrorWriter(_options.Log);
            var docsets = ConfigLoader.FindDocsets(errors, _workingDirectory, _options);
            if (docsets.Length == 0)
            {
                errors.Add(Errors.Config.ConfigNotFound(_workingDirectory));
                return errors.HasError;
            }

            Parallel.ForEach(docsets, docset =>
            {
                new DocsetBuilder(_workingDirectory, docset.docsetPath, docset.outputPath, _options).Build(errors);
            });

            Telemetry.TrackOperationTime("build", stopwatch.Elapsed);
            Log.Important($"Build done in {Progress.FormatTimeSpan(stopwatch.Elapsed)}", ConsoleColor.Green);
            errors.PrintSummary();
            return errors.HasError;
        }
    }
}
