// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Glob;
    using Microsoft.DocAsCode.Plugins;

    /// <summary>
    /// TODO: NOT SURE IF IT IS WORKING NOW, simply migrate from old sub command and have not done any E2E test
    /// </summary>
    internal class PackCommand : ISubCommand
    {
        private readonly PackCommandOptions _options;

        public bool AllowReplay => true;

        public PackCommand(PackCommandOptions options)
        {
            _options = options;
        }

        public void Exec(SubCommandRunningContext context)
        {
            var outputFile = Path.Combine(_options.OutputFolder ?? Environment.CurrentDirectory, _options.Name ?? "externalreference.rpk");
            var baseUri = new Uri(_options.BaseUrl);
            if (!baseUri.IsAbsoluteUri)
            {
                throw new InvalidOptionException("BaseUrl should be absolute url.", "BaseUrl");
            }

            var source = _options.Source.TrimEnd('/', '\\');
            using (var package = _options.AppendMode ? ExternalReferencePackageWriter.Append(outputFile, baseUri) : ExternalReferencePackageWriter.Create(outputFile, baseUri))
            {
                var files = FileGlob.GetFiles(source, new string[] { _options.Glob }, null).ToList();
                if (_options.FlatMode)
                {
                    ExternalReferencePackageHelper.AddFiles(package, baseUri, _options.UrlPattern, string.Empty, files);
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
                            ExternalReferencePackageHelper.AddFiles(package, baseUri, _options.UrlPattern, string.Empty, g.Files);
                        }
                        else
                        {
                            ExternalReferencePackageHelper.AddFiles(package, baseUri, _options.UrlPattern, g.Folder + "/", g.Files);
                        }
                    }
                }
            }
        }
    }
}
