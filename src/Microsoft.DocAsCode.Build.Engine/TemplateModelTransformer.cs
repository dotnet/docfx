// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class TemplateModelTransformer
    {
        private const string GlobalVariableKey = "__global";

        private readonly DocumentBuildContext _context;
        private readonly ApplyTemplateSettings _settings;
        private readonly SystemMetadataGenerator _systemMetadataGenerator;
        private readonly TemplateCollection _templateCollection;
        private readonly IDictionary<string, object> _globalVariables;

        public TemplateModelTransformer(DocumentBuildContext context, TemplateCollection templateCollection, ApplyTemplateSettings settings, IDictionary<string, object> globals)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _context = context;
            _templateCollection = templateCollection;
            _settings = settings;
            _globalVariables = globals;
            _systemMetadataGenerator = new SystemMetadataGenerator(context);
        }

        /// <summary>
        /// Must guarantee thread safety
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        internal ManifestItem Transform(InternalManifestItem item)
        {
            if (item.Model == null || item.Model.Content == null) throw new ArgumentNullException("Content for item.Model should not be null!");
            var model = ConvertObjectToDictionary(item.Model.Content);
            model = AppendGlobalMetadata(model);
            if (_settings.Options.HasFlag(ApplyTemplateOptions.ExportRawModel))
            {
                ExportModel(model, item.FileWithoutExtension, _settings.RawModelExportSettings);
            }

            var manifestItem = new ManifestItem
            {
                DocumentType = item.DocumentType,
                SourceRelativePath = item.LocalPathFromRoot,
                Metadata = item.Metadata,
            };
            var outputDirectory = _settings.OutputFolder ?? Directory.GetCurrentDirectory();

            // 1. process resource
            if (item.ResourceFile != null)
            {
                // Resource file has already been processed in its plugin
                var ofi = new OutputFileInfo
                {
                    RelativePath = item.ResourceFile,
                    LinkToPath = GetLinkToPath(item.ResourceFile),
                };
                manifestItem.OutputFiles.Add("resource", ofi);
            }

            // 2. process model
            var templateBundle = _templateCollection[item.DocumentType];
            if (templateBundle == null)
            {
                return manifestItem;
            }

            HashSet<string> missingUids = new HashSet<string>();

            // Must convert to JObject first as we leverage JsonProperty as the property name for the model
            foreach (var template in templateBundle.Templates)
            {
                if (!template.ContainsTemplateRenderer)
                {
                    continue;
                }
                try
                {
                    var extension = template.Extension;
                    string outputFile = item.FileWithoutExtension + extension;
                    object viewModel = null;
                    try
                    {
                        viewModel = template.TransformModel(model);
                    }
                    catch (Exception e)
                    {
                        string message;
                        if (_settings.DebugMode)
                        {
                            // save raw model for further investigation:
                            var rawModelPath = ExportModel(model, item.FileWithoutExtension, _settings.RawModelExportSettingsForDebug);
                            message = $"Error transforming model \"{rawModelPath}\" generated from \"{item.LocalPathFromRoot}\" using \"{template.ScriptName}\". {e.Message}";
                        }
                        else
                        {
                            message = $"Error transforming model generated from \"{item.LocalPathFromRoot}\" using \"{template.ScriptName}\". To get the detailed raw model, please run docfx with debug mode --debug. {e.Message} ";
                        }

                        Logger.LogError(message);
                        throw new DocumentException(message, e);
                    }

                    string result;
                    try
                    {
                        result = template.Transform(viewModel);
                    }
                    catch (Exception e)
                    {
                        string message;
                        if (_settings.DebugMode)
                        {
                            // save view model for further investigation:
                            var viewModelPath = ExportModel(viewModel, outputFile, _settings.ViewModelExportSettingsForDebug);
                            message = $"Error applying template \"{template.Name}\" to view model \"{viewModelPath}\" generated from \"{item.LocalPathFromRoot}\". {e.Message}";
                        }
                        else
                        {
                            message = $"Error applying template \"{template.Name}\" generated from \"{item.LocalPathFromRoot}\". To get the detailed view model, please run docfx with debug mode --debug. {e.Message}";
                        }

                        Logger.LogError(message);
                        throw new DocumentException(message, e);
                    }

                    if (_settings.Options.HasFlag(ApplyTemplateOptions.ExportViewModel))
                    {
                        ExportModel(viewModel, outputFile, _settings.ViewModelExportSettings);
                    }

                    if (_settings.Options.HasFlag(ApplyTemplateOptions.TransformDocument))
                    {
                        if (string.IsNullOrWhiteSpace(result))
                        {
                            string message;
                            if (_settings.DebugMode)
                            {
                                var viewModelPath = ExportModel(viewModel, outputFile, _settings.ViewModelExportSettingsForDebug);
                                message = $"Model \"{viewModelPath}\" is transformed to empty string with template \"{template.Name}\"";
                            }
                            else
                            {
                                message = $"Model is transformed to empty string with template \"{template.Name}\". To get the detailed view model, please run docfx with debug mode --debug";
                            }
                            Logger.LogWarning(message);
                        }

                        TransformDocument(result ?? string.Empty, extension, _context, outputFile, missingUids, manifestItem);
                        Logger.LogDiagnostic($"Transformed model \"{item.LocalPathFromRoot}\" to \"{outputFile}\".");
                    }
                }
                catch (PathTooLongException e)
                {
                    var message = $"Error processing {item.LocalPathFromRoot}: {e.Message}";
                    throw new PathTooLongException(message, e);
                }

            }

            if (missingUids.Count > 0)
            {
                var uids = string.Join(", ", missingUids.Select(s => $"\"{s}\""));
                Logger.LogWarning($"Invalid cross reference {uids}.", null, item.LocalPathFromRoot);
            }

            return manifestItem;
        }

        private string GetLinkToPath(string fileName)
        {
            if (EnvironmentContext.FileAbstractLayerImpl == null)
            {
                return null;
            }
            var pp = ((FileAbstractLayer)EnvironmentContext.FileAbstractLayerImpl).GetOutputPhysicalPath(fileName);
            var expandPP = Path.GetFullPath(Environment.ExpandEnvironmentVariables(pp));
            var outputPath = Path.GetFullPath(_context.BuildOutputFolder);
            if (expandPP.Length > outputPath.Length &&
                (expandPP[outputPath.Length] == '\\' || expandPP[outputPath.Length] == '/') &&
                FilePathComparer.OSPlatformSensitiveStringComparer.Equals(outputPath, expandPP.Remove(outputPath.Length)))
            {
                return null;
            }
            else
            {
                return pp;
            }
        }

        private IDictionary<string, object> AppendGlobalMetadata(IDictionary<string, object> model)
        {
            if (_globalVariables == null)
            {
                return model;
            }

            if (model.ContainsKey(GlobalVariableKey))
            {
                Logger.LogWarning($"Data model contains key {GlobalVariableKey}, {GlobalVariableKey} is to keep system level global metadata and is not allowed to overwrite. The {GlobalVariableKey} property inside data model will be ignored.");
            }

            // Create a new object with __global property, the shared model does not contain __global property
            var appended = new Dictionary<string, object>(model);
            appended[GlobalVariableKey] = _globalVariables;
            return appended;
        }

        private static IDictionary<string, object> ConvertObjectToDictionary(object model)
        {
            var dictionary = model as IDictionary<string, object>;
            if (dictionary != null)
            {
                return dictionary;
            }

            var objectModel = ConvertToObjectHelper.ConvertStrongTypeToObject(model) as IDictionary<string, object>;
            if (objectModel == null)
            {
                throw new ArgumentException("Only object model is supported for template transformation.");
            }

            return objectModel;
        }

        private static string ExportModel(object model, string modelFileRelativePath, ExportSettings settings)
        {
            if (model == null)
            {
                return null;
            }
            var outputFolder = settings.OutputFolder ?? string.Empty;
            string modelPath;
            try
            {
                modelPath = Path.GetFullPath(Path.Combine(outputFolder, settings.PathRewriter(modelFileRelativePath)));
            }
            catch (PathTooLongException)
            {
                modelPath = Path.GetFullPath(Path.Combine(outputFolder, Path.GetRandomFileName()));
            }

            JsonUtility.Serialize(modelPath, model);
            return StringExtension.ToDisplayPath(modelPath);
        }

        private void TransformDocument(string result, string extension, IDocumentBuildContext context, string destFilePath, HashSet<string> missingUids, ManifestItem manifestItem)
        {
            Task<byte[]> hashTask;
            using (var stream = EnvironmentContext.FileAbstractLayer.Create(destFilePath).WithMd5Hash(out hashTask))
            using (var sw = new StreamWriter(stream))
            {
                if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        TransformHtml(context, result, manifestItem.SourceRelativePath, destFilePath, sw);
                    }
                    catch (AggregateException e)
                    {
                        e.Handle(s =>
                        {
                            var xrefExcetpion = s as CrossReferenceNotResolvedException;
                            if (xrefExcetpion != null)
                            {
                                missingUids.Add(xrefExcetpion.UidRawText);
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        });
                    }
                }
                else
                {
                    sw.Write(result);
                }
            }
            var ofi = new OutputFileInfo
            {
                RelativePath = destFilePath,
                LinkToPath = GetLinkToPath(destFilePath),
                Hash = Convert.ToBase64String(hashTask.Result)
            };
            if (!string.IsNullOrEmpty(_context.VersionOutputFolder))
            {
                ofi.VersionName = _context.VersionName;
            }
            manifestItem.OutputFiles.Add(extension, ofi);
        }

        private void TransformHtml(IDocumentBuildContext context, string html, string sourceFilePath, string destFilePath, StreamWriter outputWriter)
        {
            // Update href and xref
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            document.LoadHtml(html);

            var xrefExceptions = TransformHtmlCore(context, sourceFilePath, destFilePath, document);

            document.Save(outputWriter);
            if (xrefExceptions.Count > 0)
            {
                throw new AggregateException(xrefExceptions);
            }
        }

        private List<CrossReferenceNotResolvedException> TransformHtmlCore(IDocumentBuildContext context, string sourceFilePath, string destFilePath, HtmlAgilityPack.HtmlDocument html)
        {
            var xrefLinkNodes = html.DocumentNode.SelectNodes("//a[starts-with(@href, 'xref:')]");
            if (xrefLinkNodes != null)
            {
                foreach (var xref in xrefLinkNodes)
                {
                    TransformXrefLink(xref, context);
                }
            }

            var xrefExceptions = new List<CrossReferenceNotResolvedException>();
            var xrefNodes = html.DocumentNode.SelectNodes("//xref")?
                .Where(s => s.GetAttributeValue("href", null) != null || s.GetAttributeValue("uid", null) != null).ToList();
            if (xrefNodes != null)
            {
                foreach (var xref in xrefNodes)
                {
                    try
                    {
                        UpdateXref(xref, context, Constants.DefaultLanguage);
                    }
                    catch (CrossReferenceNotResolvedException e)
                    {
                        xrefExceptions.Add(e);
                    }
                }
            }

            var srcNodes = html.DocumentNode.SelectNodes("//*/@src");
            if (srcNodes != null)
                foreach (var link in srcNodes)
                {
                    UpdateHref(link, "src", context, sourceFilePath, destFilePath);
                }

            var hrefNodes = html.DocumentNode.SelectNodes("//*/@href");
            if (hrefNodes != null)
            {
                foreach (var link in hrefNodes)
                {
                    UpdateHref(link, "href", context, sourceFilePath, destFilePath);
                }
            }

            return xrefExceptions;
        }

        private static void TransformXrefLink(HtmlAgilityPack.HtmlNode node, IDocumentBuildContext context)
        {
            var convertedNode = XRefDetails.ConvertXrefLinkNodeToXrefNode(node);
            node.ParentNode.ReplaceChild(convertedNode, node);
        }

        private static void UpdateXref(HtmlAgilityPack.HtmlNode node, IDocumentBuildContext context, string language)
        {
            var xref = XRefDetails.From(node);

            // Resolve external xref map first, and then internal xref map.
            // Internal one overrides external one
            var xrefSpec = context.GetXrefSpec(xref.Uid);
            xref.ApplyXrefSpec(xrefSpec);

            var convertedNode = xref.ConvertToHtmlNode(language);
            node.ParentNode.ReplaceChild(convertedNode, node);
            if (xrefSpec == null)
            {
                if (xref.ThrowIfNotResolved)
                {
                    throw new CrossReferenceNotResolvedException(xref.Uid, xref.RawSource, null);
                }
            }
        }

        private void UpdateHref(HtmlAgilityPack.HtmlNode link, string attribute, IDocumentBuildContext context, string sourceFilePath, string destFilePath)
        {
            var originalHref = link.GetAttributeValue(attribute, null);
            var anchor = link.GetAttributeValue("anchor", null);
            link.Attributes.Remove("anchor");
            var originalPath = UriUtility.GetPath(originalHref);
            var path = RelativePath.TryParse(originalPath);

            if (path == null)
            {
                return;
            }

            var fli = new FileLinkInfo
            {
                FromFileInSource = sourceFilePath,
                FromFileInDest = destFilePath,
            };

            if (path.IsFromWorkingFolder())
            {
                var targetInSource = path.UrlDecode();
                fli.ToFileInSource = targetInSource.RemoveWorkingFolder();
                fli.ToFileInDest = RelativePath.GetPathWithoutWorkingFolderChar(context.GetFilePath(targetInSource));
                fli.FileLinkInSource = targetInSource - (RelativePath)sourceFilePath;
                if (fli.ToFileInDest != null)
                {
                    var resolved = (RelativePath)fli.ToFileInDest - (RelativePath)destFilePath;
                    fli.FileLinkInDest = resolved;
                    fli.Href = resolved.UrlEncode();
                }
                else
                {
                    fli.Href = (targetInSource.RemoveWorkingFolder() - ((RelativePath)sourceFilePath).RemoveWorkingFolder()).UrlEncode();
                }
            }
            else
            {
                fli.FileLinkInSource = path.UrlDecode();
                fli.ToFileInSource = ((RelativePath)sourceFilePath + path).RemoveWorkingFolder();
                fli.FileLinkInDest = fli.FileLinkInSource;
                fli.Href = originalPath;
            }
            var href = _settings.HrefGenerator?.GenerateHref(fli) ?? fli.Href;
            link.SetAttributeValue(attribute, href + UriUtility.GetQueryString(originalHref) + (anchor ?? UriUtility.GetFragment(originalHref)));
        }

        private struct FileLinkInfo : IFileLinkInfo
        {
            public string Href { get; set; }

            public string FromFileInDest { get; set; }

            public string FromFileInSource { get; set; }

            public string ToFileInDest { get; set; }

            public string ToFileInSource { get; set; }

            public string FileLinkInSource { get; set; }

            public string FileLinkInDest { get; set; }
        }
    }
}
