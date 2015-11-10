// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.Utility;
    using System;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// TODO: NOT SURE IF IT IS WORKING NOW, simply migrate from old sub command and have not done any E2E test
    /// </summary>
    internal class PackCommand : ICommand
    {
        public PackCommandOptions _options { get; }
        public Options _rootOptions { get; }
        public PackCommand(Options options)
        {
            _options = options.PackCommand;
            _rootOptions = options;
        }

        public ParseResult Exec(RunningContext context)
        {
            var outputFile = Path.Combine(_options.OutputFolder ?? Environment.CurrentDirectory, _options.Name ?? "externalreference.rpk");
            try
            {
                var baseUri = new Uri(_options.BaseUrl);
                if (!baseUri.IsAbsoluteUri)
                {
                    return new ParseResult(ResultLevel.Error, "BaseUrl should be absolute url.");
                }
                var source = _options.Source.TrimEnd('/', '\\');
                using (var package = _options.AppendMode ? ExternalReferencePackageWriter.Append(outputFile, baseUri) : ExternalReferencePackageWriter.Create(outputFile, baseUri))
                {
                    var files = GlobPathHelper.GetFiles(source, _options.Glob).ToList();
                    if (_options.FlatMode)
                    {
                        package.AddFiles(string.Empty, files);
                    }
                    else
                    {
                        foreach (var g in from f in files
                                          group f by Path.GetDirectoryName(f) into g
                                          select new
                                          {
                                              Folder = g.Key.Substring(source.Length).Replace('\\', '/').Trim('/'),
                                              Files = g.ToList(),
                                          })
                        {
                            if (g.Folder.Length == 0)
                            {
                                package.AddFiles(string.Empty, g.Files);
                            }
                            else
                            {
                                package.AddFiles(g.Folder + "/", g.Files);
                            }
                        }
                    }
                }
                return ParseResult.SuccessResult;
            }
            catch (Exception ex)
            {
                return new ParseResult(ResultLevel.Error, ex.ToString());
            }
        }
    }
}
