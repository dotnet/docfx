// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.Docs.Build
{
    internal class LanguageServerBuilder
    {
        private readonly Builder _builder;
        private readonly ErrorList _errorList;
        private readonly Channel<FileActionEvent> _eventChannel;
        private readonly ILanguageServer _languageServer;
        private readonly LanguageServerPackage _languageServerPackage;
        private readonly PathString _workingDirectory;

        public LanguageServerBuilder(
            string workingDirectory, CommandLineOptions options, Channel<FileActionEvent> eventChannel, ILanguageServer languageServer, Package package)
        {
            _workingDirectory = new PathString(workingDirectory);
            _languageServer = languageServer;
            _errorList = new ErrorList();
            _languageServerPackage = new LanguageServerPackage(new MemoryPackage(workingDirectory), package);
            _builder = new Builder(_errorList, workingDirectory, options, _languageServerPackage);
            _eventChannel = eventChannel;
        }

        public async Task StartAsync()
        {
            while (await _eventChannel.Reader.WaitToReadAsync())
            {
                var needRebuildFiles = false;
                while (_eventChannel.Reader.TryRead(out var @event))
                {
                    switch (@event.Type)
                    {
                        case FileActionType.Opened:
                        case FileActionType.Updated:
                            UpdateMemoryPackage(new PathString(@event.FilePath), @event.Content!);
                            needRebuildFiles = true;
                            break;
                    }
                }
                if (needRebuildFiles)
                {
                    RebuildFiles();
                }
            }
        }

        private void UpdateMemoryPackage(PathString filePath, string content)
        {
            // TODO: ignore config file change
            _languageServerPackage.AddOrUpdate(filePath, content);
        }

        private void RemoveDiagnosticsOnFile(PathString filePath)
        {
            var fullPath = Path.Combine(_workingDirectory, filePath);
            _languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
            {
                Uri = DocumentUri.File(fullPath),
                Diagnostics = new Container<Diagnostic>(),
            });
        }

        private void RebuildFiles()
        {
            var filesToBuild = _languageServerPackage.GetAllFilesInMemory();
            _builder.Build(filesToBuild.Select(f => f.Value).ToArray());

            PublishDiagnosticsParams(filesToBuild);
        }

        private void PublishDiagnosticsParams(IEnumerable<PathString> files)
        {
            foreach (var file in files)
            {
                if (file.StartsWithPath(_workingDirectory, out var relativePath))
                {
                    var diagnostics = _errorList
                        .Where(error => error.Source != null
                            && error.Source.File.Path == relativePath)
                        .Select(ConvertToDiagnostics);
                    _languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
                    {
                        Uri = DocumentUri.File(file),
                        Diagnostics = new Container<Diagnostic>(diagnostics),
                    });
                }
            }
        }

        private Diagnostic ConvertToDiagnostics(Error error)
        {
            var source = error.Source!;
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
