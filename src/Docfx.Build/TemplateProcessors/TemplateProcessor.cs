// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.Engine;

public class TemplateProcessor
{
    private readonly ResourceFileReader _resourceProvider;
    private readonly TemplateCollection _templateCollection;
    private readonly DocumentBuildContext _context;
    private readonly int _maxParallelism;

    public IDictionary<string, string> Tokens { get; }

    /// <summary>
    /// TemplateName can be either file or folder
    /// 1. If TemplateName is file, it is considered as the default template
    /// 2. If TemplateName is a folder, files inside the folder is considered as the template, each file is named after {DocumentType}.{extension}
    /// </summary>
    /// <param name="templateName"></param>
    /// <param name="resourceProvider"></param>
    public TemplateProcessor(ResourceFileReader resourceProvider, DocumentBuildContext context, int maxParallelism = 0)
    {
        if (maxParallelism <= 0)
        {
            maxParallelism = Environment.ProcessorCount;
        }

        _context = context;
        _resourceProvider = resourceProvider;
        _maxParallelism = maxParallelism;
        _templateCollection = new TemplateCollection(resourceProvider, context, maxParallelism);
        if (_templateCollection.Count == 0)
        {
            Logger.LogWarning("No template bundles were found, no template will be applied to the documents. 1) Check your docfx.json 2) the templates subfolder exists inside your application folder or your docfx.json directory.");
        }
        Tokens = TemplateProcessorUtility.LoadTokens(resourceProvider) ?? new Dictionary<string, string>();
    }

    public TemplateBundle GetTemplateBundle(string documentType)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentType);

        return _templateCollection[documentType];
    }

    public bool TryGetFileExtension(string documentType, out string fileExtension)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentType);

        fileExtension = string.Empty;
        if (_templateCollection.Count == 0) return false;
        var templateBundle = _templateCollection[documentType];

        // Get default template extension
        if (templateBundle == null) return false;

        fileExtension = templateBundle.Extension;
        return true;
    }

    internal List<ManifestItem> Process(List<InternalManifestItem> manifest, ApplyTemplateSettings settings, IDictionary<string, object> globals = null)
    {
        globals ??= Tokens.ToDictionary(pair => pair.Key, pair => (object)pair.Value);
        settings ??= _context?.ApplyTemplateSettings;

        Logger.LogInfo($"Applying templates to {manifest.Count} model(s)...");
        var documentTypes = new HashSet<string>(manifest.Select(s => s.DocumentType));
        var notSupportedDocumentTypes = documentTypes.Where(s => s != "Resource" && _templateCollection[s] == null).OrderBy(s => s);
        if (notSupportedDocumentTypes.Any())
        {
            Logger.LogWarning(
                $"There is no template processing document type(s): {StringExtension.ToDelimitedString(notSupportedDocumentTypes)}",
                code: WarningCodes.Build.UnknownContentTypeForTemplate);
        }

        return ProcessCore(manifest, settings, globals);
    }

    public void CopyTemplateResources(ApplyTemplateSettings settings)
    {
        if (settings.Options.HasFlag(ApplyTemplateOptions.TransformDocument))
        {
            CopyTemplateResources(settings.OutputFolder, _templateCollection.Values);
        }
        else
        {
            Logger.LogInfo("Dryrun, no template will be applied to the documents.");
        }
    }

    private void CopyTemplateResources(string outputDirectory, IEnumerable<TemplateBundle> templateBundles)
    {
        foreach (var resourceInfo in templateBundles.SelectMany(s => s.Resources).Distinct())
        {
            _context.CancellationToken.ThrowIfCancellationRequested();

            var resourceKey = resourceInfo.ResourceKey;

            try
            {
                using var stream = _resourceProvider.GetResourceStream(resourceKey);

                if (stream is FileStream fileStream)
                {
                    CopyTemplateResourceFile(new FileInfo(fileStream.Name), outputDirectory, resourceKey);
                }
                else
                {
                    CopyTemplateResourceStream(stream, outputDirectory, resourceKey);
                }
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Info, $"Unable to get relative resource for {resourceKey}: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Copy template resource from stream.
    /// </summary>
    private static void CopyTemplateResourceStream(Stream stream, string outputDirectory, string filePath)
    {
        if (stream != null)
        {
            var path = Path.Combine(outputDirectory, filePath);
            Directory.CreateDirectory(Path.GetFullPath(Path.GetDirectoryName(path)));

            using (var writer = new FileStream(path, new FileStreamOptions
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                Share = FileShare.None,
                PreallocationSize = stream.Length,
            }))
            {
                stream.CopyTo(writer);
            }

            Logger.Log(LogLevel.Verbose, $"Saved resource {filePath} that template dependants on to {path}");
        }
        else
        {
            Logger.Log(LogLevel.Info, $"Unable to get relative resource for {filePath}");
        }
    }

    /// <summary>
    /// Copy template resource as file.
    /// </summary>
    private static void CopyTemplateResourceFile(FileInfo inputFileInfo, string outputDirectory, string filePath)
    {
        Debug.Assert(inputFileInfo.Exists);

        var path = Path.Combine(outputDirectory, filePath);
        var outputFileInfo = new FileInfo(path);
        outputFileInfo.Directory.Create();

        bool isOverwrite = outputFileInfo.Exists;

        // Skip file copy if following condition met.
        bool skipFileCopy = isOverwrite                                                        // Output file is already exists.
                         && inputFileInfo.Length == outputFileInfo.Length                      // File's lengths are identical.
                         && inputFileInfo.LastWriteTimeUtc == outputFileInfo.LastWriteTimeUtc; // File's LastWriteTimeUtc are identical.
        if (skipFileCopy)
        {
            return;
        }
        else
        {
            File.Copy(inputFileInfo.FullName, outputFileInfo.FullName, isOverwrite); // Use `File.Copy` API to preserve LastWriteTime.
        }
    }

    private List<ManifestItem> ProcessCore(List<InternalManifestItem> items, ApplyTemplateSettings settings, IDictionary<string, object> globals)
    {
        var manifest = new ConcurrentBag<ManifestItem>();
        var transformer = new TemplateModelTransformer(_context, _templateCollection, settings, globals);
        items.RunAll(
            item =>
            {
                using (new LoggerFileScope(item.LocalPathFromRoot))
                {
                    manifest.Add(transformer.Transform(item));
                }
            },
            _maxParallelism,
            _context.CancellationToken);
        return manifest.ToList();

    }
}
