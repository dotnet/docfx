// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.Docs.Build;

internal class LanguageServerBuilder
{
    private const int DebounceTimeout = 500;

    private readonly ILogger _logger;
    private readonly Builder _builder;
    private readonly Channel<bool> _buildChannel = Channel.CreateUnbounded<bool>();
    private readonly DiagnosticPublisher _diagnosticPublisher;
    private readonly LanguageServerPackage _languageServerPackage;
    private readonly ILanguageServerNotificationListener _notificationListener;
    private readonly IServiceProvider _serviceProvider;
    private readonly PathString _workingDirectory;
    private List<PathString> _filesWithDiagnostics = new();

    public LanguageServerBuilder(
        ILoggerFactory loggerFactory,
        CommandLineOptions options,
        DiagnosticPublisher diagnosticPublisher,
        LanguageServerPackage languageServerPackage,
        LanguageServerCredentialProvider languageServerCredentialProvider,
        ILanguageServerNotificationListener notificationListener,
        IServiceProvider serviceProvider)
    {
        options.DryRun = true;

        _workingDirectory = languageServerPackage.BasePath;
        _diagnosticPublisher = diagnosticPublisher;
        _languageServerPackage = languageServerPackage;
        _notificationListener = notificationListener;
        _serviceProvider = serviceProvider;
        _logger = loggerFactory.CreateLogger<LanguageServerBuilder>();
        _builder = new(options, _languageServerPackage, languageServerCredentialProvider.GetCredentials);
    }

    public void QueueBuild()
    {
        _buildChannel.Writer.TryWrite(true);
    }

    public async Task Run(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await WaitToTriggerBuild(cancellationToken);

                using var progressReporter = await CreateProgressReporter();

                progressReporter.Report("Start build...");
                var operation = Telemetry.StartMetricOperation("realTimeBuild");

                var errors = new ErrorList();
                var filesToBuild = _languageServerPackage.GetAllFilesInMemory();

                // This is to avoid the task await deadlock in the credential refresh scenario
                // The progress reporter create request task can only be completed when the current build done if there is no `Task.Yield`
                // The current build can only be completed when the credential refresh request response get handled.
                // But the responses of language server are handled sequentially, which cause the deadlock.
                await Task.Yield();
                Telemetry.SetIsRealTimeBuild(true);
                _builder.Build(errors, progressReporter, filesToBuild.Select(f => f.Value).ToArray());

                PublishDiagnosticsParams(errors, filesToBuild);

                operation.Complete();
                _notificationListener.OnNotificationHandled();

                progressReporter.Report("Build finished");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to handle build request");
                _notificationListener.OnException(ex);
                Telemetry.TrackException(ex);
            }
            finally
            {
                Telemetry.SetIsRealTimeBuild(false);
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
                using var timeout = new CancellationTokenSource(DebounceTimeout);
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
                await _buildChannel.Reader.ReadAsync(cts.Token);
                _notificationListener.OnNotificationHandled();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task<LanguageServerProgressReporter> CreateProgressReporter()
    {
        var languageServer = _serviceProvider.GetService<ILanguageServer>();
        Debug.Assert(languageServer != null);
        return new LanguageServerProgressReporter(await languageServer.WorkDoneManager.Create(
            new WorkDoneProgressBegin()
            {
                Title = "Docs real-time validation",
            }));
    }

    private void PublishDiagnosticsParams(ErrorList errors, IEnumerable<PathString> filesToBuild)
    {
        List<PathString> filesWithDiagnostics = new();
        var diagnosticsGroupByFile = from error in errors.ToArray()
                                     where error.Level != ErrorLevel.Info
                                     let source = error.Source ?? new SourceInfo(new FilePath(".openpublishing.publish.config.json"), 0, 0)
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
        var documentUrl = error.DocumentUrl ?? "https://review.docs.microsoft.com/en-us/help/contribute/validation-ref/doc-not-available?branch=main";
        return new Diagnostic
        {
            Range = new(
                 new(ConvertLocation(source.Line), ConvertLocation(source.Column)),
                 new(ConvertLocation(source.EndLine), ConvertLocation(source.EndColumn))),
            Code = error.Code,
            CodeDescription = Uri.TryCreate(documentUrl, UriKind.Absolute, out var href)
                ? new() { Href = href }
                : null,
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

        static int ConvertLocation(int original)
        {
            var target = original - 1;
            return target < 0 ? 0 : target;
        }
    }
}
