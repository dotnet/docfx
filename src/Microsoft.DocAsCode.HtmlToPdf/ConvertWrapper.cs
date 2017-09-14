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
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.HtmlToPdf.Transformer;
    using Microsoft.DocAsCode.Plugins;

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

        public static void PrerequisiteCheck()
        {
            if (!CommandUtility.ExistCommand(Constants.PdfCommandName))
            {
                throw new DocfxException(Constants.PdfCommandNotExistMessage);
            }
        }

        public void Convert()
        {
            var basePath = Path.GetFullPath(_pdfOptions.SourceDirectory);
            try
            {
                ConvertCore(basePath);
            }
            finally
            {
                if (!_pdfOptions.KeepRawFiles)
                {
                    if (Directory.Exists(basePath))
                    {
                        FolderUtility.ForceDeleteDirectoryWithAllSubDirectories(basePath);
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private void ConvertCore(string basePath)
        {
            Logger.LogVerbose($"Reading files from {basePath}");
            HtmlTransformer(basePath);
            var manifest = JsonUtility.Deserialize<Manifest>(Path.Combine(basePath, ManifestConstants.ManifestFileName));
            if (manifest == null)
            {
                Logger.LogError("Manifest file is not found.");
                throw new FileNotFoundException("Manifest file is not found.");
            }

            if (manifest.Files == null || manifest.Files.Count == 0)
            {
                Logger.LogWarning($"There is no file in manifest under {_pdfOptions.SourceDirectory}");
                return;
            }

            var tocFiles = FindTocInManifest(manifest);
            if (tocFiles.Count > 0)
            {
                Logger.LogVerbose($"Found {tocFiles.Count} TOC.json files, will generate pdf.");
            }
            else
            {
                Logger.LogWarning($"No TOC file is included, no PDF file will be generated.");
                return;
            }

            var tocHtmls = new ConcurrentBag<string>();

            IDictionary<string, PdfInformation> pdfInformations = new ConcurrentDictionary<string, PdfInformation>();

            var manifestItems = manifest.Files.Where(f => IsType(f, ManifestItemType.Content)).ToArray();
            var manifestUrlCache = new UrlCache(basePath, manifestItems);

            Parallel.ForEach(
                tocFiles,
                new ParallelOptions { MaxDegreeOfParallelism = _pdfOptions.PdfConvertParallelism },
                tocFile =>
                {
                    var tocJson = ManifestUtility.GetRelativePath(tocFile, OutputType.TocJson);
                    var tocAssetId = ManifestUtility.GetAssetId(tocFile);
                    try
                    {
                        Logger.LogVerbose($"Starting to handle {tocJson}.");
                        var tocFilePath = NormalizeFilePath(tocJson);
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
                            var pdfNameFragments = new List<string> { _pdfOptions.PdfDocsetName };
                            if (!string.IsNullOrEmpty(_pdfOptions.Locale))
                            {
                                pdfNameFragments.Add(_pdfOptions.Locale);
                            }

                            // TODO: not consistent with OPS logic Path.ChangeExtension(tocAssetId, FileExtensions.PdfExtension).Replace('/', '_')
                            // TODO: need to update the logic in OPS when merging with OPS
                            if (!string.IsNullOrWhiteSpace(tocAssetId))
                            {
                                var subFolder = Path.GetDirectoryName(tocAssetId);
                                if (!string.IsNullOrEmpty(subFolder))
                                {
                                    pdfNameFragments.Add(subFolder.Replace('/', '_'));
                                }
                            }

                            var pdfName = pdfNameFragments.ToDelimitedString("_") + FileExtensions.PdfExtension;
                            ConvertCore(basePath, pdfName, htmlModels);
                            Logger.LogInfo($"{pdfName} is generated based on {tocJson} under folder {_pdfOptions.DestDirectory}");
                            pdfInformations.Add(
                                pdfName,
                                new PdfInformation
                                {
                                    DocsetName = _pdfOptions.PdfDocsetName,
                                    TocFiles = new string[] { tocFile.SourceRelativePath },
                                    Version = tocFile.Group,
                                    AssetId = tocAssetId,
                                });
                        }
                        else
                        {
                            Logger.LogVerbose($"Skipped to convert {tocJson} to pdf because of custom exclude tocs.");
                        }
                        Logger.LogVerbose($"Finished to handle {tocJson}.");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Error happen when converting {tocJson} to Pdf. Details: {ex.Message}");
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

        private string NormalizeFilePath(string relativePath)
        {
            return relativePath.Replace('/', '\\').ToLower();
        }

        private IList<ManifestItem> FindTocInManifest(Manifest manifest)
        {
            return manifest.Files.Where(f => IsType(f, ManifestItemType.Toc)).ToList();
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

        private IList<TocModel> LoadTocModels(string basePath, ManifestItem tocFile)
        {
            return JsonUtility.Deserialize<IList<TocModel>>(Path.Combine(basePath, ManifestUtility.GetRelativePath(tocFile, OutputType.TocJson)));
        }

        private IEnumerable<string> GetManifestHtmls(Manifest manifest)
        {
            return from file in manifest.Files
                   where file != null && IsType(file, ManifestItemType.Content)
                   let outputPath = ManifestUtility.GetRelativePath(file, OutputType.Html)
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
                    UserStyleSheet = _pdfOptions.CssFilePath,
                    LoadErrorHandling = _pdfOptions.LoadErrorHandling,
                    AdditionalArguments = _pdfOptions.AdditionalPdfCommandArgs
                });

            converter.Save(Path.Combine(_pdfOptions.DestDirectory, pdfFileName));
        }

        private bool IsType(ManifestItem item, ManifestItemType targetType)
        {
            return ManifestUtility.GetDocumentType(item) == targetType;
        }

        #endregion
    }
}
