// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text;
using Docfx.Common;
using Docfx.Plugins;
using HtmlAgilityPack;

using EnvironmentVariables = Docfx.DataContracts.Common.Constants.EnvironmentVariables;

namespace Docfx.Build.Engine;

sealed class HtmlPostProcessor : IPostProcessor
{
    private static readonly UTF8Encoding Utf8EncodingWithoutBom = new(false);

    public List<IHtmlDocumentHandler> Handlers { get; } = [];

    private bool _handlerInitialized;

    public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
    {
        if (!_handlerInitialized)
        {
            Handlers.Add(new ValidateBookmark());

            bool keepDebugInfo = false;
            var docfxKeepDebugInfo = EnvironmentVariables.KeepDebugInfo;
            if (!string.IsNullOrEmpty(docfxKeepDebugInfo) && bool.TryParse(docfxKeepDebugInfo, out keepDebugInfo))
            {
                Logger.LogVerbose($"DOCFX_KEEP_DEBUG_INFO is set to {keepDebugInfo}");
            }
            if (!keepDebugInfo)
            {
                Handlers.Add(new RemoveDebugInfo());
            }
            _handlerInitialized = true;
        }

        return metadata;
    }

    public Manifest Process(Manifest manifest, string outputFolder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(outputFolder);

        foreach (var handler in Handlers)
        {
            manifest = handler.PreHandle(manifest);
        }
        foreach (var tuple in from item in manifest.Files ?? Enumerable.Empty<ManifestItem>()
                              from output in item.Output
                              where output.Key.Equals(".html", StringComparison.OrdinalIgnoreCase)
                              select new
                              {
                                  Item = item,
                                  InputFile = item.SourceRelativePath,
                                  OutputFile = output.Value.RelativePath,
                              })
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!EnvironmentContext.FileAbstractLayer.Exists(tuple.OutputFile))
            {
                continue;
            }
            var document = new HtmlDocument();
            try
            {
                using var stream = EnvironmentContext.FileAbstractLayer.OpenRead(tuple.OutputFile);
                document.Load(stream, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Warning: Can't load content from {tuple.OutputFile}: {ex.Message}");
                continue;
            }
            foreach (var handler in Handlers)
            {
                handler.Handle(document, tuple.Item, tuple.InputFile, tuple.OutputFile);
            }
            using (var stream = EnvironmentContext.FileAbstractLayer.Create(tuple.OutputFile))
            {
                document.Save(stream, Utf8EncodingWithoutBom);
            }
        }
        foreach (var handler in Handlers)
        {
            manifest = handler.PostHandle(manifest);
        }
        return manifest;
    }
}
