// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class Builder
    {
        private readonly ErrorBuilder _errors;
        private readonly string _workingDirectory;
        private readonly CommandLineOptions _options;
        private readonly Watch<DocsetBuilder[]> _docsets;

        public Builder(ErrorBuilder errors, string workingDirectory, CommandLineOptions options)
        {
            _workingDirectory = workingDirectory;
            _options = options;
            _errors = errors;
            _docsets = new Watch<DocsetBuilder[]>(LoadDocsets);
        }

        public static bool Run(string workingDirectory, CommandLineOptions options)
        {
            var stopwatch = Stopwatch.StartNew();

            using var errors = new ErrorWriter(options.Log);

            new Builder(errors, workingDirectory, options).Build();

            Telemetry.TrackOperationTime("build", stopwatch.Elapsed);
            Log.Important($"Build done in {Progress.FormatTimeSpan(stopwatch.Elapsed)}", ConsoleColor.Green);

            errors.PrintSummary();
            return errors.HasError;
        }

        public void Build()
        {
            try
            {
                Watcher.StartActivity();

                Parallel.ForEach(_docsets.Value, docset => docset.Build());
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                _errors.AddRange(dex);
            }
        }

        private DocsetBuilder[] LoadDocsets()
        {
            var docsets = ConfigLoader.FindDocsets(_errors, _workingDirectory, _options);
            if (docsets.Length == 0)
            {
                _errors.Add(Errors.Config.ConfigNotFound(_workingDirectory));
            }

            return (from docset in docsets
                    let item = DocsetBuilder.Create(_errors, _workingDirectory, docset.docsetPath, docset.outputPath, _options)
                    where item != null
                    select item).ToArray();
        }
    }
}
