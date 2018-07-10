// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class TemplateModelTransformer
    {
        private const string GlobalVariableKey = "__global";
        private const int MaxInvalidXrefMessagePerFile = 10;

        private readonly DocumentBuildContext _context;
        private readonly ApplyTemplateSettings _settings;
        private readonly TemplateCollection _templateCollection;
        private readonly RendererLoader _rendererLoader;
        private readonly IDictionary<string, object> _globalVariables;

        public TemplateModelTransformer(DocumentBuildContext context, TemplateCollection templateCollection, ApplyTemplateSettings settings, IDictionary<string, object> globals)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _templateCollection = templateCollection;
            _settings = settings;
            _globalVariables = globals;
            _rendererLoader = new RendererLoader(templateCollection.Reader, templateCollection.MaxParallelism);
        }

        /// <summary>
        /// Must guarantee thread safety
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        internal ManifestItem Transform(InternalManifestItem item)
        {
            if (item.Model == null || item.Model.Content == null)
            {
                throw new ArgumentNullException("Content for item.Model should not be null!");
            }

            var model = ConvertObjectToDictionary(item.Model.Content);
            AppendGlobalMetadata(model);

            if (_settings.Options.HasFlag(ApplyTemplateOptions.ExportRawModel))
            {
                ExportModel(model, item.FileWithoutExtension, _settings.RawModelExportSettings);
            }

            var manifestItem = new ManifestItem
            {
                DocumentType = item.DocumentType,
                SourceRelativePath = item.LocalPathFromRoot,
                Metadata = item.Metadata,
                Version = _context.VersionName,
                Group = _context.GroupInfo?.Name,
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

            var unresolvedXRefs = new List<XRefDetails>();

            // Must convert to JObject first as we leverage JsonProperty as the property name for the model
            foreach (var template in templateBundle.Templates)
            {
                if (template.Renderer == null)
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
                            Logger.LogWarning(message, code: WarningCodes.Build.EmptyOutputFiles);
                        }

                        List<XRefDetails> invalidXRefs;
                        TransformDocument(result ?? string.Empty, extension, _context, outputFile, manifestItem, out invalidXRefs);
                        unresolvedXRefs.AddRange(invalidXRefs);
                        Logger.LogDiagnostic($"Transformed model \"{item.LocalPathFromRoot}\" to \"{outputFile}\".");
                    }
                }
                catch (PathTooLongException e)
                {
                    var message = $"Error processing {item.LocalPathFromRoot}: {e.Message}";
                    throw new PathTooLongException(message, e);
                }
            }

            item.Model = null;

            LogInvalidXRefs(unresolvedXRefs);

            return manifestItem;
        }

        private void LogInvalidXRefs(List<XRefDetails> unresolvedXRefs)
        {
            if (unresolvedXRefs == null || unresolvedXRefs.Count == 0)
            {
                return;
            }

            var distinctUids = unresolvedXRefs.Select(i => i.RawSource ?? i.Uid).Distinct().Select(s => $"\"{HttpUtility.HtmlDecode(s)}\"").ToList();
            Logger.LogWarning(
                $"{distinctUids.Count} invalid cross reference(s) {distinctUids.ToDelimitedString(", ")}.",
                code: WarningCodes.Build.UidNotFound);
            foreach (var group in unresolvedXRefs.GroupBy(i => i.SourceFile))
            {
                // For each source file, print the first 10 invalid cross reference
                var details = group.Take(MaxInvalidXrefMessagePerFile).Select(i => $"\"{HttpUtility.HtmlDecode(i.RawSource)}\" in line {i.SourceStartLineNumber.ToString()}").Distinct().ToList();
                var prefix = details.Count > MaxInvalidXrefMessagePerFile ? $"top {MaxInvalidXrefMessagePerFile} " : string.Empty;
                var message = $"Details for {prefix}invalid cross reference(s): {details.ToDelimitedString(", ")}";

                if (group.Key != null)
                {
                    Logger.LogInfo(message, file: group.Key);
                }
                else
                {
                    Logger.LogInfo(message);
                }
            }
        }

        private string GetLinkToPath(string fileName)
        {
            if (EnvironmentContext.FileAbstractLayerImpl == null)
            {
                return null;
            }
            string pp;
            try
            {
                pp = ((FileAbstractLayer)EnvironmentContext.FileAbstractLayerImpl).GetOutputPhysicalPath(fileName);
            }
            catch (FileNotFoundException)
            {
                pp = ((FileAbstractLayer)EnvironmentContext.FileAbstractLayerImpl).GetPhysicalPath(fileName);
            }
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

        private void AppendGlobalMetadata(IDictionary<string, object> model)
        {
            if (_globalVariables == null)
            {
                return;
            }

            if (model.ContainsKey(GlobalVariableKey))
            {
                Logger.LogWarning($"Data model contains key {GlobalVariableKey}, {GlobalVariableKey} is to keep system level global metadata and is not allowed to overwrite. The {GlobalVariableKey} property inside data model will be ignored.");
            }

            model[GlobalVariableKey] = new Dictionary<string, object>(_globalVariables);
        }

        private static IDictionary<string, object> ConvertObjectToDictionary(object model)
        {
            if (model is IDictionary<string, object> dictionary)
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

        private void TransformDocument(string result, string extension, IDocumentBuildContext context, string destFilePath, ManifestItem manifestItem, out List<XRefDetails> unresolvedXRefs)
        {
            Task<byte[]> hashTask;
            unresolvedXRefs = new List<XRefDetails>();
            using (var stream = EnvironmentContext.FileAbstractLayer.Create(destFilePath).WithMd5Hash(out hashTask))
            using (var sw = new StreamWriter(stream))
            {
                if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase))
                {
                    TransformHtml(context, result, manifestItem.SourceRelativePath, destFilePath, sw, out unresolvedXRefs);
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
            manifestItem.OutputFiles.Add(extension, ofi);
        }

        private void TransformHtml(IDocumentBuildContext context, string html, string sourceFilePath, string destFilePath, StreamWriter outputWriter, out List<XRefDetails> unresolvedXRefs)
        {
            // Update href and xref
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            TransformHtmlCore(context, sourceFilePath, destFilePath, document, out unresolvedXRefs);

            document.Save(outputWriter);
        }

        private void TransformHtmlCore(IDocumentBuildContext context, string sourceFilePath, string destFilePath, HtmlDocument html, out List<XRefDetails> unresolvedXRefs)
        {
            unresolvedXRefs = new List<XRefDetails>();
            var xrefLinkNodes = html.DocumentNode.SelectNodes("//a[starts-with(@href, 'xref:')]");
            if (xrefLinkNodes != null)
            {
                foreach (var xref in xrefLinkNodes)
                {
                    TransformXrefLink(xref, context);
                }
            }

            var xrefNodes = html.DocumentNode.SelectNodes("//xref");
            if (xrefNodes != null)
            {
                foreach (var xref in xrefNodes)
                {
                    var resolved = UpdateXref(xref, context, Constants.DefaultLanguage, out var xrefDetails);
                    if (!resolved)
                    {
                        unresolvedXRefs.Add(xrefDetails);
                    }
                }
            }

            var srcNodes = html.DocumentNode.SelectNodes("//*/@src");
            if (srcNodes != null)
            {
                foreach (var link in srcNodes)
                {
                    UpdateHref(link, "src", context, sourceFilePath, destFilePath);
                }
            }

            var hrefNodes = html.DocumentNode.SelectNodes("//*/@href");
            if (hrefNodes != null)
            {
                foreach (var link in hrefNodes)
                {
                    UpdateHref(link, "href", context, sourceFilePath, destFilePath);
                }
            }
        }

        private static void TransformXrefLink(HtmlNode node, IDocumentBuildContext context)
        {
            var convertedNode = XRefDetails.ConvertXrefLinkNodeToXrefNode(node);
            node.ParentNode.ReplaceChild(convertedNode, node);
        }

        private bool UpdateXref(HtmlNode node, IDocumentBuildContext context, string language, out XRefDetails xref)
        {
            xref = XRefDetails.From(node);
            XRefSpec xrefSpec = null;
            if (!string.IsNullOrEmpty(xref.Uid))
            {
                // Resolve external xref map first, and then internal xref map.
                // Internal one overrides external one
                xrefSpec = context.GetXrefSpec(HttpUtility.HtmlDecode(xref.Uid));
                xref.ApplyXrefSpec(xrefSpec);
            }

            var renderer = xref.TemplatePath == null ? null : _rendererLoader.Load(xref.TemplatePath);
            var convertedNode = xref.ConvertToHtmlNode(language, renderer);
            node.ParentNode.ReplaceChild(convertedNode, node);
            if (xrefSpec == null && xref.ThrowIfNotResolved == true)
            {
                return false;
            }

            return true;
        }

        private void UpdateHref(HtmlNode link, string attribute, IDocumentBuildContext context, string sourceFilePath, string destFilePath)
        {
            var originalHref = link.GetAttributeValue(attribute, null);
            var path = UriUtility.GetPath(originalHref);
            var anchorFromNode = link.GetAttributeValue("anchor", null);
            var segments = anchorFromNode ?? UriUtility.GetQueryStringAndFragment(originalHref);
            link.Attributes.Remove("anchor");
            if (RelativePath.TryParse(path) == null)
            {
                if (!string.IsNullOrEmpty(anchorFromNode))
                {
                    link.SetAttributeValue(attribute, originalHref + anchorFromNode);
                }
                return;
            }
            var fli = FileLinkInfo.Create(sourceFilePath, destFilePath, path, context);

            // fragment and query in original href takes precedence over the one from hrefGenerator
            var href = _settings.HrefGenerator?.GenerateHref(fli);
            link.SetAttributeValue(
                attribute,
                href == null ? fli.Href + segments : UriUtility.MergeHref(href, segments));
        }
    }
}
