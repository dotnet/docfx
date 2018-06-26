// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal sealed class Report : IDisposable
    {
        private readonly object _outputLock = new object();
        private Lazy<TextWriter> _output;
        private Dictionary<string, ErrorLevel> _rules;

        public void Configure(string docsetPath, Config config)
        {
            Debug.Assert(_output == null, "Cannot change report output path");

            _rules = config.Rules;
            _output = new Lazy<TextWriter>(() =>
            {
                var outputFilePath = Path.GetFullPath(Path.Combine(docsetPath, config.Output.Path, "build.log"));

                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

                return File.CreateText(outputFilePath);
            });
        }

        public void Write(Error error)
        {
            var level = _rules != null && _rules.TryGetValue(error.Code, out var overrideLevel) ? overrideLevel : error.Level;
            if (level == ErrorLevel.Off)
            {
                return;
            }

            if (_output != null)
            {
                lock (_outputLock)
                {
                    _output.Value.WriteLine(error.ToString(level));
                }
            }

            Log.WriteError(level, error);
        }

        public void Dispose()
        {
            lock (_outputLock)
            {
                if (_output != null && _output.IsValueCreated)
                {
                    _output.Value.Dispose();
                }
            }
        }
    }
}
