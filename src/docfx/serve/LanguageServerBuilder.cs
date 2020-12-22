// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.Docs.Build
{
    internal class LanguageServerBuilder
    {
        private readonly ILogger _logger;
        private readonly Builder _builder;
        private readonly Channel<bool> _buildChannel = Channel.CreateUnbounded<bool>();
        private readonly DiagnosticPublisher _diagnosticPublisher;
        private readonly LanguageServerPackage _languageServerPackage;
        private readonly ILanguageServerNotificationListener _notificationListener;
        private readonly PathString _workingDirectory;
        private List<PathString> _filesWithDiagnostics = new();

        public LanguageServerBuilder(
            ILoggerFactory loggerFactory,
            CommandLineOptions options,
            DiagnosticPublisher diagnosticPublisher,
            LanguageServerPackage languageServerPackage,
            LanguageServerCredentialRefresher credentialRefresher,
            ILanguageServerNotificationListener notificationListener)
        {
            options.DryRun = true;

            _workingDirectory = languageServerPackage.BasePath;
            _diagnosticPublisher = diagnosticPublisher;
            _languageServerPackage = languageServerPackage;
            _notificationListener = notificationListener;
            _logger = loggerFactory.CreateLogger<LanguageServerBuilder>();
            _builder = new(languageServerPackage.BasePath, options, _languageServerPackage, credentialRefresher.GetRefreshedToken);
        }

        public void QueueBuild()
        {
            _buildChannel.Writer.TryWrite(true);
        }

        public async void Run(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await WaitToTriggerBuild(cancellationToken);

                    var errors = new ErrorList();
                    var filesToBuild = _languageServerPackage.GetAllFilesInMemory();
                    _builder.Build(errors, filesToBuild.Select(f => f.Value).ToArray());

                    PublishDiagnosticsParams(errors, filesToBuild);
                    _notificationListener.OnNotificationHandled();
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Failed to handle build request");
                }
            }
        }

        private async Task WaitToTriggerBuild(CancellationToken cancellationToken)
        {
            await _buildChannel.Reader.ReadAsync(cancellationToken);

            try
            {
                while (true)
                {
                    using var timeout = new CancellationTokenSource(1000);
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
                    await _buildChannel.Reader.ReadAsync(cts.Token);
                    _notificationListener.OnNotificationHandled();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void PublishDiagnosticsParams(ErrorList errors, IEnumerable<PathString> filesToBuild)
        {
            List<PathString> filesWithDiagnostics = new();
            var diagnosticsGroupByFile = from error in errors
                                         let source = error.Source
                                         where source != null
                                         let diagnostic = ConvertToDiagnostics(error, source)
                                         group diagnostic by source.File;
            foreach (var diagnostics in diagnosticsGroupByFile)
            {
                var fullPath = _workingDirectory.Concat(diagnostics.Key.Path);
                filesWithDiagnostics.Add(fullPath);
                _diagnosticPublisher.PublishDiagnostic(fullPath, diagnostics.ToList());
            }

            foreach (var fileWithoutDiagnostics in filesToBuild.Union(_filesWithDiagnostics).Except(filesWithDiagnostics))
            {
                _diagnosticPublisher.PublishDiagnostic(fileWithoutDiagnostics, new List<Diagnostic>());
            }

            _filesWithDiagnostics = filesWithDiagnostics;
        }

        private static Diagnostic ConvertToDiagnostics(Error error, SourceInfo source)
        {
            return new Diagnostic
            {
                Range = new(
                     new(ConvertLocation(source.Line), ConvertLocation(source.Column)),
                     new(ConvertLocation(source.EndLine), ConvertLocation(source.EndColumn))),
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
