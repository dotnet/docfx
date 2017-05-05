// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.PdfConverter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.DataContracts.Common;

    internal class ConverterImpl
    {
        #region Fields

        private const string HtmlExtension = ".html";
        private readonly PdfOptions _pdfOptions;

        #endregion

        #region Constructor

        public ConverterImpl(PdfOptions pdfOptions)
        {
            _pdfOptions = pdfOptions;
        }

        #endregion

        #region Public Methods

        public void Convert(Manifest manifest, string outputFolder)
        {
            using (new LoggerPhaseScope("Convert PDF", LogLevel.Info))
            {
                var tg = new TocGenerator(manifest);

                // Generate toc.html
                var toc = tg.GenerateTableOfContent(outputFolder);

                var basePath = outputFolder;
                var htmlModels = GetInTocContentFiles(toc).GroupBy(s => s.HtmlFilePath).Select(s => s.First()).ToList();

                htmlModels.Insert(0, new HtmlModel { Title = "Cover Page", HtmlFilePath = tg.RootTocHtmlRelativePath });
                ConvertCore(basePath, $"{_pdfOptions.PdfDocsetName}.pdf", htmlModels);

                var tocHtmls = new HashSet<string>(htmlModels.Select(s => s.Href));
                var otherConceptuals = GetNotInTocContentFiles(manifest, tocHtmls);
                if (_pdfOptions.GenerateAppendices)
                {
                    if (otherConceptuals.Count > 0)
                    {
                        var items = new List<HtmlModel>(){
                            new HtmlModel
                        {
                            Title = "appendices",
                            Children = otherConceptuals.Select((other, index) => new HtmlModel
                            {
                                Title = $"Appendix {index + 1}",
                                HtmlFilePath = other
                            }).ToList()
                        } };
                        Logger.LogVerbose($"Generating {otherConceptuals.Count} not-in-toc files into appendices.");
                        ConvertCore(basePath, $"{_pdfOptions.PdfDocsetName}_appendices.pdf", items);
                    }
                }
                else
                {
                    Logger.LogVerbose($"{otherConceptuals.Count} files are not included in pdf: {otherConceptuals.ToDelimitedString()}.");
                }
            }
        }

        #endregion

        #region Private Methods

        private IEnumerable<HtmlModel> GetInTocContentFiles(TocItemViewModel toc)
        {
            if (toc == null)
            {
                yield break;
            }

            var model = new HtmlModel
            {
                Title = toc.Name,
                Href = toc.Href
            };
            if (PathUtility.IsRelativePath(toc.Href))
            {
                model.HtmlFilePath = UriUtility.GetPath(toc.Href);
            }
            else
            {
                model.ExternalLink = toc.Href;
            }

            yield return model;

            if (toc.Items != null)
            {
                foreach (var item in toc.Items)
                {
                    foreach (var i in GetInTocContentFiles(item))
                    {
                        yield return i;
                    }
                }
            }
        }

        private IList<string> GetNotInTocContentFiles(Manifest manifest, HashSet<string> tocHtmls)
        {
            return (from file in manifest.Files
                    where file != null && IsType(file.DocumentType, ManifestItemType.Content)
                    && file.OutputFiles != null && file.OutputFiles.ContainsKey(HtmlExtension)
                    let outputPath = file.OutputFiles[HtmlExtension].RelativePath
                    where !string.IsNullOrEmpty(outputPath) && !tocHtmls.Contains(outputPath)
                    select outputPath into p
                    orderby p
                    select p).Distinct().ToList();
        }

        private bool IsType(string documentType, ManifestItemType type)
        {
            if (documentType == null)
            {
                return false;
            }

            if (documentType.Equals(type.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (type == ManifestItemType.Content)
            {
                return !IsType(documentType, ManifestItemType.Resource) && !IsType(documentType, ManifestItemType.Toc);
            }

            return false;
        }

        private void ConvertCore(string basePath, string pdfFileName, IList<HtmlModel> htmlModels)
        {
            using (new LoggerPhaseScope("$Generating {pdfFileName} to {basePath}"))
            {
                var converter = new HtmlToPdfConverter(
                    htmlModels,
                    new HtmlToPdfOptions
                    {
                        BasePath = basePath,
                        UserStyleSheet = _pdfOptions.CssFilePath
                    });

                converter.Save(Path.Combine(_pdfOptions.DestDirectory ?? string.Empty, pdfFileName));
            }
        }

        private enum ManifestItemType
        {
            Content,
            Resource,
            Toc
        }
        #endregion
    }
}
