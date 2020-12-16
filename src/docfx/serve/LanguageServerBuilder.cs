// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private readonly SemaphoreSlim _buildSemaphore;
        private readonly ILanguageServer _languageServer;
        private readonly LanguageServerPackage _languageServerPackage;
        private readonly PathString _workingDirectory;

        public LanguageServerBuilder(
            string workingDirectory,
            CommandLineOptions options,
            SemaphoreSlim buildSemaphore,
            ILanguageServer languageServer,
            LanguageServerPackage languageServerPackage)
        {
            options.DryRun = true;

            _workingDirectory = new(workingDirectory);
            _languageServer = languageServer;
            _errorList = new();
            _languageServerPackage = languageServerPackage;
            _builder = new(_errorList, workingDirectory, options, _languageServerPackage);
            _buildSemaphore = buildSemaphore;
        }

        public async Task StartAsync()
        {
            while (true)
            {
                await _buildSemaphore.WaitAsync();
                var filesToBuild = _languageServerPackage.GetAllFilesInMemory();
                _builder.Build(filesToBuild.Select(f => f.Value).ToArray());

                PublishDiagnosticsParams(filesToBuild);
            }
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

                    _languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
                    {
                        Uri = DocumentUri.File(file),
                        Diagnostics = new Container<Diagnostic>(diagnostics.ToList()),
                    });
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
