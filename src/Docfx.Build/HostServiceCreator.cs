// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;

using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.Engine;

class HostServiceCreator
{
    private readonly DocumentBuildContext _context;

    public HostServiceCreator(DocumentBuildContext context)
    {
        _context = context;
    }

    public virtual HostService CreateHostService(
        DocumentBuildParameters parameters,
        TemplateProcessor templateProcessor,
        IMarkdownService markdownService,
        IEnumerable<IInputMetadataValidator> metadataValidator,
        IDocumentProcessor processor,
        IEnumerable<FileAndType> files)
    {
        var (models, invalidFiles) = LoadModels(files, parameters, processor);
        var hostService = new HostService(
            models,
            parameters.VersionName,
            parameters.VersionDir,
            parameters.GroupInfo)
        {
            MarkdownService = markdownService,
            Processor = processor,
            Template = templateProcessor,
            Validators = metadataValidator?.ToImmutableList(),
            InvalidSourceFiles = invalidFiles.ToImmutableList(),
        };
        return hostService;
    }

    public virtual (FileModel model, bool valid) Load(
        IDocumentProcessor processor,
        ImmutableDictionary<string, object> metadata,
        FileMetadata fileMetadata,
        FileAndType file)
    {
        using (new LoggerFileScope(file.File))
        {
            Logger.LogDiagnostic($"Processor {processor.Name}, File {file.FullPath}: Loading...");

            var fileMeta = NeedApplyMetadata()
                ? ApplyFileMetadata(file.FullPath, metadata, fileMetadata)
                : ImmutableDictionary<string, object>.Empty;
            try
            {
                return (processor.Load(file, fileMeta), true);
            }
            catch (DocumentException)
            {
                return (null, false);
            }
            catch (Exception e)
            {
                Logger.LogError(
                    $"Unable to load file '{file.File}' via processor '{processor.Name}': {e.Message}",
                    code: ErrorCodes.Build.InvalidInputFile);
                return (null, false);
            }
        }

        bool NeedApplyMetadata()
        {
            return file.Type != DocumentType.Resource;
        }
    }

    private (IEnumerable<FileModel> models, IEnumerable<string> invalidFiles) LoadModels(IEnumerable<FileAndType> files, DocumentBuildParameters parameters, IDocumentProcessor processor)
    {
        if (files == null)
        {
            return (Enumerable.Empty<FileModel>(), Enumerable.Empty<string>());
        }

        var models = new ConcurrentBag<FileModel>();
        var invalidFiles = new ConcurrentBag<string>();
        files.RunAll(file =>
        {
            var (model, valid) = Load(processor, parameters.Metadata, parameters.FileMetadata, file);
            if (model != null)
            {
                models.Add(model);
            }
            if (!valid)
            {
                invalidFiles.Add(file.File);
            }
        },
        _context.MaxParallelism,
        _context.CancellationToken);

        return (models.OrderBy(m => m.File, StringComparer.Ordinal).ToArray(), invalidFiles);
    }

    private static ImmutableDictionary<string, object> ApplyFileMetadata(
        string file,
        ImmutableDictionary<string, object> metadata,
        FileMetadata fileMetadata)
    {
        if (fileMetadata == null || fileMetadata.Count == 0)
        {
            return metadata;
        }

        var result = new Dictionary<string, object>(metadata);
        var baseDir = string.IsNullOrEmpty(fileMetadata.BaseDir) ? Directory.GetCurrentDirectory() : fileMetadata.BaseDir;
        var relativePath = PathUtility.MakeRelativePath(baseDir, file);
        foreach (var item in fileMetadata)
        {
            // As the latter one overrides the former one, match the pattern from latter to former
            for (int i = item.Value.Length - 1; i >= 0; i--)
            {
                if (item.Value[i].Glob.Match(relativePath))
                {
                    // override global metadata if metadata is defined in file metadata
                    result[item.Value[i].Key] = item.Value[i].Value;
                    Logger.LogDiagnostic($"{relativePath} matches file metadata with glob pattern {item.Value[i].Glob.Raw} for property {item.Value[i].Key}");
                    break;
                }
            }
        }
        return result.ToImmutableDictionary();
    }
}
