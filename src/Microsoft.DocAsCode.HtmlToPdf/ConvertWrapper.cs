// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.HtmlToPdf.Transformer;

    public class ConvertWrapper
    {
        #region Fields

        private const string TmpFolder = ".tmp";
        private const string HtmlFilePattern = "*.html";
        private const string TocPageFileName = "TOC.html";
        private readonly PdfOptions _pdfOptions;

        #endregion

        #region Constructor

        public ConvertWrapper(PdfOptions pdfOptions)
        {
            _pdfOptions = pdfOptions;
        }
        #endregion

        #region Public Methods

        public void Convert()
        {
            using (new LoggerPhaseScope("Convert PDF"))
            {
                var fullPathTmpFolder = Path.Combine(_pdfOptions.SourceDirectory, TmpFolder);
                if (Directory.Exists(fullPathTmpFolder))
                {
                    FolderUtility.ForceDeleteDirectoryWithAllSubDirectories(fullPathTmpFolder);
                }
                using (var folder = new SelfCleaningFolder(fullPathTmpFolder))
                {
                    FolderUtility.CopyDirectoryWithAllSubDirectories(_pdfOptions.SourceDirectory, folder.FullPath);
                    HtmlTransformer(folder.FullPath);
                    var manifest = JsonUtility.FromJsonString<Manifest>(folder.FullPath);
                    if (manifest == null)
                    {
                        Logger.LogError("Manifest file is not found.");
                        throw new FileNotFoundException("Manifest file is not found.");
                    }

                    if (manifest.Files == null || manifest.Files.Length == 0)
                    {
                        Logger.LogWarning($"There is no file in manifest under {_pdfOptions.SourceDirectory}");
                        return;
                    }

                    var basePath = folder.FullPath;
                    var tocFiles = FindTocInManifest(manifest);

                    Logger.LogVerbose($"Found {tocFiles.Count} TOC.json files, will generate pdf.");
                    var tocHtmls = new ConcurrentBag<string>();

                    IDictionary<string, PdfInformation> pdfInformations = new ConcurrentDictionary<string, PdfInformation>();

                    var manifestItems = manifest.Files.Where(f => f.Type == ManifestItemType.Content).ToArray();
                    var manifestUrlCache = new UrlCache(basePath, manifestItems);

                    Parallel.ForEach(
                        tocFiles,
                        new ParallelOptions { MaxDegreeOfParallelism = _pdfOptions.PdfConvertParallelism },
                        tocFile =>
                        {
                            try
                            {
                                Logger.LogVerbose($"Starting to handle {tocFile.Output.TocJson.RelativePath}.");
                                var tocFilePath = NormalizeFilePath(tocFile.Output.TocJson.RelativePath);
                                var tocPageFilePath = Path.Combine(basePath, Path.GetDirectoryName(tocFilePath), TocPageFileName);
                                var tocModels = LoadTocModels(basePath, tocFile) ?? new List<TocModel>();

                                var crruentTocHtmls = new ConcurrentBag<string>();
                                var htmlModels = BuildHtmlModels(basePath, tocModels, crruentTocHtmls);

                                HtmlNotInTocTransformer(basePath, manifestUrlCache, crruentTocHtmls);

                                if (_pdfOptions.GenerateAppendices)
                                {
                                    crruentTocHtmls.AsParallel().ForAll(tocHtmls.Add);
                                }

                                if (File.Exists(tocPageFilePath))
                                {
                                    RemoveQueryStringAndBookmarkTransformer(tocPageFilePath);
                                    AbsolutePathInTocPageFileTransformer(tocPageFilePath);
                                    htmlModels.Insert(0, new HtmlModel { Title = "Cover Page", HtmlFilePath = tocPageFilePath });
                                }
                                if (_pdfOptions.ExcludeTocs == null || _pdfOptions.ExcludeTocs.All(p => NormalizeFilePath(p) != tocFilePath))
                                {
                                    var pdfName = _pdfOptions.PdfDocsetName + "_" + _pdfOptions.Locale + "_" + Path.ChangeExtension(tocFile.AssetId, FileExtensions.PdfExtension).Replace('/', '_');
                                    Logger.LogVerbose($"Starting to convert {tocFile.Output.TocJson.RelativePath} to {pdfName}.");

                                    ConvertCore(basePath, pdfName, htmlModels);
                                    pdfInformations.Add(
                                        pdfName,
                                        new PdfInformation
                                        {
                                            DocsetName = _pdfOptions.PdfDocsetName,
                                            AssetId = tocFile.AssetId,
                                            Version = tocFile.Version
                                        });
                                    Logger.LogVerbose($"Finished to convert {tocFile.Output.TocJson.RelativePath} to {pdfName}.");
                                }
                                else
                                {
                                    Logger.LogVerbose($"Skipped to convert {tocFile.Output.TocJson.RelativePath} to pdf because of custom exclude tocs.");
                                }
                                Logger.LogVerbose($"Finished to handle {tocFile.Output.TocJson.RelativePath}.");
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning($"Error happen when converting {tocFile.Output.TocJson.RelativePath} to Pdf. Details: {ex.Message}");
                            }
                        });

                    using (var fileStream = new FileStream(Path.Combine(_pdfOptions.DestDirectory, _pdfOptions.PdfDocsetName + FileExtensions.JsonExtension), FileMode.Create, FileAccess.Write))
                    {
                        using (var ws = new StreamWriter(fileStream))
                        {
                            JsonUtility.Serialize(ws, pdfInformations);
                        }
                    }

                    if (_pdfOptions.GenerateAppendices)
                    {
                        var otherConceptuals = ManifestHtmlsExceptTocHtmls(manifest, tocHtmls);
                        if (otherConceptuals.Count > 0)
                        {
                            var htmlModels = new List<HtmlModel>();
                            var htmlModel = new HtmlModel
                            {
                                Title = "appendices",
                                Children = otherConceptuals.Select((other, index) => new HtmlModel
                                {
                                    Title = $"Appendix {index + 1}",
                                    HtmlFilePath = other
                                }).ToList()
                            };
                            htmlModels.Add(htmlModel);
                            Logger.LogVerbose("Starting to convert appendices to pdf.");
                            ConvertCore(basePath, "appendices", htmlModels);
                        }
                    }
                    else
                    {
                        Logger.LogVerbose("Skipped to convert appendices to pdf.");
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private string NormalizeFilePath(string relativePath)
        {
            return relativePath.Replace('/', '\\').ToLower();
        }

        private IList<ManifestItemWithAssetId> FindTocInManifest(Manifest manifest)
        {
            return manifest.Files.Where(p => p.Type == ManifestItemType.Toc).ToList();
        }

        private void HtmlTransformer(string fullPath)
        {
            var htmlFiles = Directory.GetFiles(fullPath, HtmlFilePattern, SearchOption.AllDirectories);

            ITransformer transformer = new FrameTransformer();
            transformer.Transform(htmlFiles);
        }

        private void HtmlNotInTocTransformer(string basePath, UrlCache manifestUrlCache, ConcurrentBag<string> tocHtmls)
        {
            if (_pdfOptions.NeedGeneratePdfExternalLink)
            {
                ITransformer transformer = new HtmlNotInTocTransformer(basePath, manifestUrlCache, _pdfOptions);
                transformer.Transform(tocHtmls.Distinct());
            }
        }

        private void RemoveQueryStringAndBookmarkTransformer(string tocPageFilePath)
        {
            ITransformer transformer = new RemoveQueryStringTransformer();
            transformer.Transform(new List<string> { tocPageFilePath });
        }

        private void AbsolutePathInTocPageFileTransformer(string tocPage)
        {
            if (_pdfOptions.NeedGeneratePdfExternalLink)
            {
                ITransformer transformer = new AbsolutePathInTocPageFileTransformer(_pdfOptions);
                transformer.Transform(new List<string> { tocPage });
            }
        }

        private T LoadFromFilePath<T>(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                return JsonUtility.FromJsonString<T>(reader.ReadToEnd());
            }
        }

        private IList<TocModel> LoadTocModels(string basePath, ManifestItemWithAssetId tocFile)
        {
            return LoadFromFilePath<IList<TocModel>>(Path.Combine(basePath, tocFile.Output.TocJson.RelativePath));
        }

        private IEnumerable<string> GetManifestHtmls(Manifest manifest)
        {
            return from file in manifest.Files
                   where file != null && file.Type == ManifestItemType.Content && file.Output != null && file.Output.Html != null
                   let outputPath = file.Output.Html.RelativePath
                   where !string.IsNullOrEmpty(outputPath)
                   select outputPath;
        }

        private void ConvertTocModelToHtmlModel(IList<TocModel> tocModels, IList<HtmlModel> htmlModels, ConcurrentBag<string> tocHtmls)
        {
            foreach (var tocModel in tocModels)
            {
                var htmlModel = new HtmlModel
                {
                    Title = tocModel.Title,
                    ExternalLink = tocModel.ExternalLink,
                    HtmlFilePath = tocModel.HtmlFilePath.RemoveUrlQueryString().RemoveUrlBookmark(),
                    Children = new List<HtmlModel>()
                };
                if (!string.IsNullOrEmpty(tocModel.HtmlFilePath))
                {
                    tocHtmls.Add(tocModel.HtmlFilePath.RemoveUrlQueryString().RemoveUrlBookmark());
                }

                if (tocModel.Children != null && tocModel.Children.Any())
                {
                    ConvertTocModelToHtmlModel(tocModel.Children, htmlModel.Children, tocHtmls);
                }

                htmlModels.Add(htmlModel);
            }
        }

        private IList<string> ManifestHtmlsExceptTocHtmls(Manifest manifest, ConcurrentBag<string> tocHtmls)
        {
            var manifestConceptuals = GetManifestHtmls(manifest);
            var others = manifestConceptuals.Where(p => !tocHtmls.Contains(p)).Distinct().ToList();
            return others;
        }

        private IList<HtmlModel> BuildHtmlModels(string basePath, IList<TocModel> tocModels, ConcurrentBag<string> tocHtmls)
        {
            var htmlModels = new List<HtmlModel>();
            ConvertTocModelToHtmlModel(tocModels, htmlModels, tocHtmls);
            return htmlModels;
        }

        private void ConvertCore(string basePath, string pdfFileName, IList<HtmlModel> htmlModels)
        {
            var converter = new HtmlToPdfConverter(
                htmlModels,
                new HtmlToPdfOptions
                {
                    BasePath = basePath,
                    UserStyleSheet = _pdfOptions.CssFilePath
                });

            converter.Save(Path.Combine(_pdfOptions.DestDirectory, pdfFileName));
        }

        #endregion
    }
}
