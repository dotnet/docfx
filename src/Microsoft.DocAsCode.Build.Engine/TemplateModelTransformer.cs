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
    using Microsoft.DocAsCode.Utility;

    using TypeForwardedToRelativePath = Microsoft.DocAsCode.Common.RelativePath;
    using TypeForwardedToStringExtension = Microsoft.DocAsCode.Common.StringExtension;

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
                OutputFiles = new Dictionary<string, OutputFileInfo>(),
                Metadata = item.Metadata,
            };
            var outputDirectory = _settings.OutputFolder ?? Directory.GetCurrentDirectory();

            // 1. process resource
            if (item.ResourceFile != null)
            {
                // Resource file has already been processed in its plugin
                manifestItem.OutputFiles.Add("resource", new OutputFileInfo
                {
                    RelativePath = item.ResourceFile,
                    LinkToPath = null,
                    Hash = null
                });
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
                    string outputPath = Path.Combine(outputDirectory, outputFile);
                    var dir = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    object viewModel = null;
                    try
                    {
                        viewModel = template.TransformModel(model);
                    }
                    catch (Exception e)
                    {
                        // save raw model for further investigation:
                        var exportSettings = ApplyTemplateSettings.RawModelExportSettingsForDebug;
                        var rawModelPath = ExportModel(model, item.FileWithoutExtension, exportSettings);
                        var message = $"Error transforming model \"{rawModelPath}\" generated from \"{item.LocalPathFromRoot}\" using \"{template.ScriptName}\": {e.Message}";
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
                        // save view model for further investigation:
                        var exportSettings = ApplyTemplateSettings.ViewModelExportSettingsForDebug;
                        var viewModelPath = ExportModel(viewModel, outputFile, exportSettings);
                        var message = $"Error applying template \"{template.Name}\" to view model \"{viewModelPath}\" generated from \"{item.LocalPathFromRoot}\": {e.Message}";
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
                            // TODO: WHAT to do if is transformed to empty string? STILL creat empty file?
                            var exportSettings = ApplyTemplateSettings.ViewModelExportSettingsForDebug;
                            var viewModelPath = ExportModel(viewModel, outputFile, exportSettings);
                            Logger.LogWarning($"Model \"{viewModelPath}\" is transformed to empty string with template \"{template.Name}\"");
                            File.WriteAllText(outputPath, string.Empty);
                        }
                        else
                        {
                            TransformDocument(result, extension, _context, outputPath, outputFile, missingUids, manifestItem);
                            Logger.LogDiagnostic($"Transformed model \"{item.LocalPathFromRoot}\" to \"{outputPath}\".");
                        }
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
            if (model == null) return null;
            var outputFolder = settings.OutputFolder;

            string modelPath = Path.Combine(outputFolder ?? string.Empty, settings.PathRewriter(modelFileRelativePath));

            JsonUtility.Serialize(modelPath, model);
            return TypeForwardedToStringExtension.ToDisplayPath(modelPath);
        }

        private static void TransformDocument(string result, string extension, IDocumentBuildContext context, string outputPath, string relativeOutputPath, HashSet<string> missingUids, ManifestItem manifestItem)
        {
            var subDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(subDirectory) &&
                !Directory.Exists(subDirectory))
            {
                Directory.CreateDirectory(subDirectory);
            }

            Task<byte[]> hashTask;
            using (var stream = File.Create(outputPath).WithMd5Hash(out hashTask))
            using (var sw = new StreamWriter(stream))
            {
                if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        TransformHtml(context, result, relativeOutputPath, sw);
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
            manifestItem.OutputFiles.Add(extension, new OutputFileInfo
            {
                RelativePath = relativeOutputPath,
                LinkToPath = null,
                Hash = Convert.ToBase64String(hashTask.Result)
            });
        }

        private static void TransformHtml(IDocumentBuildContext context, string html, string relativeModelPath, StreamWriter outputWriter)
        {
            // Update href and xref
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            document.LoadHtml(html);

            var xrefExceptions = TransformHtmlCore(context, relativeModelPath, document);

            document.Save(outputWriter);
            if (xrefExceptions.Count > 0)
            {
                throw new AggregateException(xrefExceptions);
            }
        }

        private static List<CrossReferenceNotResolvedException> TransformHtmlCore(IDocumentBuildContext context, string relativeModelPath, HtmlAgilityPack.HtmlDocument html)
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
            var xrefNodes = html.DocumentNode.SelectNodes("//xref/@href");
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
                    UpdateHref(link, "src", context, relativeModelPath);
                }

            var hrefNodes = html.DocumentNode.SelectNodes("//*/@href");
            if (hrefNodes != null)
            {
                foreach (var link in hrefNodes)
                {
                    UpdateHref(link, "href", context, relativeModelPath);
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
                    throw new CrossReferenceNotResolvedException(xref.Uid, xref.Raw, null);
                }
            }
        }

        private static void UpdateHref(HtmlAgilityPack.HtmlNode link, string attribute, IDocumentBuildContext context, string relativePath)
        {
            var key = link.GetAttributeValue(attribute, null);
            string path;
            if (TypeForwardedToRelativePath.TryGetPathWithoutWorkingFolderChar(key, out path))
            {
                var href = context.GetFilePath(key);
                var anchor = link.GetAttributeValue("anchor", null);

                if (href != null)
                {
                    href = ((TypeForwardedToRelativePath)UpdateFilePath(href, relativePath)).UrlEncode();

                    if (!string.IsNullOrEmpty(anchor))
                    {
                        href += anchor;
                        link.Attributes.Remove("anchor");
                    }
                    link.SetAttributeValue(attribute, href);
                }
                else
                {
                    Logger.LogWarning($"File {path} is not found in {relativePath}.");
                    // TODO: what to do if file path not exists?
                    // CURRENT: fallback to the original one
                    path = (TypeForwardedToRelativePath)path - (TypeForwardedToRelativePath)relativePath;
                    if (!string.IsNullOrEmpty(anchor))
                    {
                        path += anchor;
                        link.Attributes.Remove("anchor");
                    }
                    link.SetAttributeValue(attribute, path);
                }
            }
        }

        private static string UpdateFilePath(string path, string modelFilePathToRoot)
        {
            if (TypeForwardedToRelativePath.IsPathFromWorkingFolder(path))
            {
                return ((TypeForwardedToRelativePath)path).RemoveWorkingFolder().MakeRelativeTo((TypeForwardedToRelativePath)modelFilePathToRoot);
            }

            return path;
        }
    }
}
