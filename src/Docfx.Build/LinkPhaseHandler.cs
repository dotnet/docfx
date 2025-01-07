// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.Engine;

internal class LinkPhaseHandler
{
    public DocumentBuildContext Context { get; }

    public TemplateProcessor TemplateProcessor { get; }

    private List<ManifestItemWithContext> _manifestWithContext;

    public LinkPhaseHandler(DocumentBuildContext context, TemplateProcessor templateProcessor)
    {
        Context = context;
        TemplateProcessor = templateProcessor;
    }

    public void Handle(List<HostService> hostServices, int maxParallelism)
    {
        Save(hostServices, maxParallelism);

        if (Context != null)
        {
            var manifestProcessor = new ManifestProcessor(_manifestWithContext, Context, TemplateProcessor);
            manifestProcessor.Process();
        }
    }

    private void Save(List<HostService> hostServices, int maxParallelism)
    {
        _manifestWithContext = [];
        foreach (var hostService in hostServices)
        {
            _manifestWithContext.AddRange(ExportManifest(hostService));
        }
    }

    private IEnumerable<ManifestItemWithContext> ExportManifest(HostService hostService)
    {
        var manifestItems = new List<ManifestItemWithContext>();
        hostService.Models.RunAll(m =>
        {
            if (m.Type != DocumentType.Overwrite)
            {
                using (new LoggerFileScope(m.LocalPathFromRoot))
                {
                    Logger.LogDiagnostic($"Processor {hostService.Processor.Name}: Saving...");
                    m.BaseDir = Context.BuildOutputFolder;
                    if (m.FileAndType.SourceDir != m.FileAndType.DestinationDir)
                    {
                        m.File = (RelativePath)m.FileAndType.DestinationDir + ((RelativePath)m.File - (RelativePath)m.FileAndType.SourceDir);
                    }
                    m.File = Path.Combine(Context.VersionFolder ?? string.Empty, m.File);
                    var result = hostService.Processor.Save(m);
                    if (result != null)
                    {
                        string extension = string.Empty;
                        if (hostService.Template != null)
                        {
                            if (hostService.Template.TryGetFileExtension(result.DocumentType, out extension))
                            {
                                m.File = result.FileWithoutExtension + extension;
                            }
                        }

                        var item = HandleSaveResult(hostService, m, result);
                        item.Extension = extension;

                        manifestItems.Add(new ManifestItemWithContext(item, m, hostService.Processor, hostService.Template?.GetTemplateBundle(result.DocumentType)));
                    }
                }
            }
        }, Context.CancellationToken);
        return manifestItems;
    }

    private InternalManifestItem HandleSaveResult(
        HostService hostService,
        FileModel model,
        SaveResult result)
    {
        Context.SetFilePath(model.Key, ((RelativePath)model.File).GetPathFromWorkingFolder());

        CheckFileLink(model, hostService, result);
        HandleUids(result);
        RegisterXRefSpec(result);

        return GetManifestItem(model, result);
    }

    private void CheckFileLink(FileModel model, HostService hostService, SaveResult result)
    {
        result.LinkToFiles.RunAll(fileLink =>
        {
            if (!hostService.SourceFiles.ContainsKey(fileLink))
            {
                if (Context.ApplyTemplateSettings.HrefGenerator != null)
                {
                    var path = ((RelativePath)fileLink).RemoveWorkingFolder() - ((RelativePath)model.OriginalFileAndType.File);
                    var fli = new FileLinkInfo
                    {
                        FromFileInSource = model.OriginalFileAndType.File,
                        FromFileInDest = model.File,
                        ToFileInSource = ((RelativePath)fileLink).RemoveWorkingFolder().ToString(),
                        FileLinkInSource = path,
                        GroupInfo = Context.GroupInfo,
                        Href = path.UrlEncode()
                    };

                    if (Context.ApplyTemplateSettings.HrefGenerator.GenerateHref(fli) != fli.Href)
                    {
                        return; // if HrefGenerator returns new href. Skip InvalidFileLink check.
                    }
                }
                if (result.FileLinkSources.TryGetValue(fileLink, out ImmutableList<LinkSourceInfo> list))
                {
                    foreach (var fileLinkSourceFile in list)
                    {
                        Logger.LogWarning(
                            $"Invalid file link:({fileLinkSourceFile.Target}{fileLinkSourceFile.Anchor}).",
                            null,
                            fileLinkSourceFile.SourceFile,
                            fileLinkSourceFile.LineNumber.ToString(),
                            WarningCodes.Build.InvalidFileLink);
                    }
                }
                else
                {
                    Logger.LogWarning($"Invalid file link:({fileLink}).", code: WarningCodes.Build.InvalidFileLink);
                }
            }
        }, Context.CancellationToken);
    }

    private void HandleUids(SaveResult result)
    {
        if (result.LinkToUids.Count > 0)
        {
            Context.XRef.UnionWith(result.LinkToUids.Where(s => s != null));
        }
    }

    private void RegisterXRefSpec(SaveResult result)
    {
        foreach (var spec in result.XRefSpecs)
        {
            if (!string.IsNullOrWhiteSpace(spec?.Uid))
            {
                Context.RegisterInternalXrefSpec(spec);
            }
        }
        foreach (var spec in result.ExternalXRefSpecs)
        {
            if (!string.IsNullOrWhiteSpace(spec?.Uid))
            {
                Context.ReportExternalXRefSpec(spec);
            }
        }
    }

    private static InternalManifestItem GetManifestItem(FileModel model, SaveResult result)
    {
        return new InternalManifestItem
        {
            DocumentType = result.DocumentType,
            FileWithoutExtension = result.FileWithoutExtension,
            ResourceFile = result.ResourceFile,
            Key = model.Key,
            LocalPathFromRoot = model.LocalPathFromRoot,
            Content = model.Content,
            InputFolder = model.OriginalFileAndType.BaseDir,
            Metadata = new Dictionary<string, object>((IDictionary<string, object>)model.ManifestProperties),
        };
    }
}
