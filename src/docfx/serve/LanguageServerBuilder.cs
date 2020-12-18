// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.Docs.Build
{
    internal class LanguageServerBuilder
    {
        private readonly Builder _builder;
        private readonly ErrorList _errorList;
        private readonly Channel<bool> _buildChannel;
        private readonly DiagnosticPublisher _diagnosticPublisher;
        private readonly LanguageServerPackage _languageServerPackage;
        private readonly PathString _workingDirectory;

        public LanguageServerBuilder(
            CommandLineOptions options,
            DiagnosticPublisher diagnosticPublisher,
            LanguageServerPackage languageServerPackage)
        {
            options.DryRun = true;

            _workingDirectory = languageServerPackage.BasePath;
            _diagnosticPublisher = diagnosticPublisher;
            _errorList = new();
            _languageServerPackage = languageServerPackage;
            _builder = new(_errorList, languageServerPackage.BasePath, options, _languageServerPackage);
            _buildChannel = Channel.CreateUnbounded<bool>();
            _ = StartAsync();
        }

        public void QueueBuild()
        {
            _buildChannel.Writer.TryWrite(true);
        }

        private async Task StartAsync()
        {
            while (true)
            {
                await WaitToTriggerBuild();
                var filesToBuild = _languageServerPackage.GetAllFilesInMemory();
                _builder.Build(filesToBuild.Select(f => f.Value).ToArray());

                PublishDiagnosticsParams(filesToBuild);
                TestQuirks.HandledEventCountIncrease?.Invoke();
            }
        }

        private async Task WaitToTriggerBuild()
        {
            await _buildChannel.Reader.ReadAsync();

            try
            {
                while (true)
                {
                    using var cts = new CancellationTokenSource(1000);
                    await _buildChannel.Reader.ReadAsync(cts.Token);
                    TestQuirks.HandledEventCountIncrease?.Invoke();
                }
            }
            catch (System.OperationCanceledException)
            {
            }
            return;
        }

        private void PublishDiagnosticsParams(IEnumerable<PathString> files)
        {
            foreach (var file in files)
            {
                if (file.StartsWithPath(_workingDirectory, out var relativePath))
                {
                    var diagnostics = from error in _errorList
                                      let source = error.Source
                                      where source != null && source.File.Path == relativePath
                                      select ConvertToDiagnostics(error, source);

                    _diagnosticPublisher.PublishDiagnostic(file, diagnostics.ToList());
                }
            }
        }

        private static Diagnostic ConvertToDiagnostics(Error error, SourceInfo source)
        {
            return new Diagnostic
            {
                Range = new Range(
                     new Position(ConvertLocation(source.Line), ConvertLocation(source.Column)),
                     new Position(ConvertLocation(source.EndLine), ConvertLocation(source.EndColumn))),
                Code = error.Code,
                Source = "Docs Validation",
                Severity = error.Level switch
                {
                    ErrorLevel.Error => DiagnosticSeverity.Error,
                    ErrorLevel.Warning => DiagnosticSeverity.Warning,
                    ErrorLevel.Suggestion => DiagnosticSeverity.Information,
                    ErrorLevel.Info => DiagnosticSeverity.Hint,
                    _ => null,
                },
                Message = error.Message,
            };

            int ConvertLocation(int original)
            {
                var target = original - 1;
                return target < 0 ? 0 : target;
            }
        }
    }
}
