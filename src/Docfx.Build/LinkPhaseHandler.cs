// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;

using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.Engine;

static class LinkPhaseHandler
{
    public static void Handle(List<HostService> hostServices, DocumentBuildContext context, TemplateProcessor templateProcessor)
    {
        var manifest = new ConcurrentBag<ManifestItemWithContext>();

        foreach (var hostService in hostServices)
        {
            foreach (var model in hostService.Models)
                Save(hostService, model);
        }

        if (context != null)
        {
            var manifestProcessor = new ManifestProcessor(manifest.ToList(), context, templateProcessor);
            manifestProcessor.Process();
        }

        void Save(HostService hostService, FileModel m)
        {
            if (m.Type is DocumentType.Overwrite)
                return;

            using var _ = new LoggerFileScope(m.LocalPathFromRoot);

            m.BaseDir = context.BuildOutputFolder;
            if (m.FileAndType.SourceDir != m.FileAndType.DestinationDir)
            {
                m.File = (RelativePath)m.FileAndType.DestinationDir + (((RelativePath)m.File) - (RelativePath)m.FileAndType.SourceDir);
            }
            m.File = Path.Combine(context.VersionFolder ?? string.Empty, m.File);

            if (hostService.Processor.Save(m) is { } result)
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

                manifest.Add(new ManifestItemWithContext(item, m, hostService.Processor, hostService.Template?.GetTemplateBundle(result.DocumentType)));
            }
        }

        InternalManifestItem HandleSaveResult(
            HostService hostService,
            FileModel model,
            SaveResult result)
        {
            context.SetFilePath(model.Key, ((RelativePath)model.File).GetPathFromWorkingFolder());
            DocumentException.RunAll(
                () => CheckFileLink(model, hostService, result),
                () => HandleUids(result),
                () => RegisterXRefSpec(result));

            return GetManifestItem(model, result);
        }

        void CheckFileLink(FileModel model, HostService hostService, SaveResult result)
        {
            result.LinkToFiles.RunAll(fileLink =>
            {
                if (!hostService.SourceFiles.ContainsKey(fileLink))
                {
                    if (context.ApplyTemplateSettings.HrefGenerator != null)
                    {
                        var path = ((RelativePath)fileLink).RemoveWorkingFolder() - ((RelativePath)model.OriginalFileAndType.File);
                        var fli = new FileLinkInfo
                        {
                            FromFileInSource = model.OriginalFileAndType.File,
                            FromFileInDest = model.File,
                            ToFileInSource = ((RelativePath)fileLink).RemoveWorkingFolder().ToString(),
                            FileLinkInSource = path,
                            GroupInfo = context.GroupInfo,
                            Href = path.UrlEncode()
                        };

                        if (context.ApplyTemplateSettings.HrefGenerator.GenerateHref(fli) != fli.Href)
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
            });
        }

        void HandleUids(SaveResult result)
        {
            if (result.LinkToUids.Count > 0)
            {
                context.XRef.UnionWith(result.LinkToUids.Where(s => s != null));
            }
        }

        void RegisterXRefSpec(SaveResult result)
        {
            foreach (var spec in result.XRefSpecs)
            {
                if (!string.IsNullOrWhiteSpace(spec?.Uid))
                {
                    context.RegisterInternalXrefSpec(spec);
                }
            }
            foreach (var spec in result.ExternalXRefSpecs)
            {
                if (!string.IsNullOrWhiteSpace(spec?.Uid))
                {
                    context.ReportExternalXRefSpec(spec);
                }
            }
        }

        static InternalManifestItem GetManifestItem(FileModel model, SaveResult result)
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
}
