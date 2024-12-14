// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using Docfx.Common;
using Docfx.Common.Git;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.PdbSourceDocument;
using Microsoft.SourceLink.Tools;

#nullable enable

namespace Docfx.Dotnet;

partial class SymbolUrlResolver
{
    private static readonly ConditionalWeakTable<IAssemblySymbol, SourceLinkProvider?> s_sourceLinkProviders = [];

    public static string? GetPdbSourceLinkUrl(Compilation compilation, ISymbol symbol)
    {
        var assembly = symbol.ContainingAssembly;
        if (assembly is null || assembly.Locations.Length == 0 || !assembly.Locations[0].IsInMetadata)
            return null;

        var rawUrl = s_sourceLinkProviders.GetValue(assembly, CreateSourceLinkProvider)?.TryGetSourceLinkUrl(symbol);

        return rawUrl is null ? null : GitUtility.RawContentUrlToContentUrl(rawUrl);

        SourceLinkProvider? CreateSourceLinkProvider(IAssemblySymbol assembly)
        {
            var pe = compilation.GetMetadataReference(assembly) as PortableExecutableReference;
            if (string.IsNullOrEmpty(pe?.FilePath) || !File.Exists(pe.FilePath))
                return null;

            var pdbPath = Path.ChangeExtension(pe.FilePath, ".pdb");
            if (!File.Exists(pdbPath))
            {
                Logger.LogVerbose($"No PDB file found for {pe.FilePath}, skip loading source link.");
                return null;
            }

            try
            {
                return new(
                    new PEReader(File.OpenRead(pe.FilePath)),
                    MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(pdbPath)));
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }
    }

    private class SourceLinkProvider : IDisposable
    {
        private readonly PEReader _peReader;
        private readonly MetadataReaderProvider _pdbReaderProvider;
        private readonly MetadataReader _dllReader;
        private readonly MetadataReader _pdbReader;

        public SourceLinkProvider(PEReader peReader, MetadataReaderProvider pdbReaderProvider)
        {
            _peReader = peReader;
            _pdbReaderProvider = pdbReaderProvider;
            _dllReader = peReader.GetMetadataReader();
            _pdbReader = pdbReaderProvider.GetMetadataReader();
        }

        public string? TryGetSourceLinkUrl(ISymbol symbol)
        {
            var entityHandle = MetadataTokens.EntityHandle(symbol.MetadataToken);
            var documentHandles = SymbolSourceDocumentFinder.FindDocumentHandles(entityHandle, _dllReader, _pdbReader);
            var sourceLinkUrls = new List<string>();

            foreach (var handle in documentHandles)
            {
                if (TryGetSourceLinkUrl(handle) is { } sourceLinkUrl)
                    sourceLinkUrls.Add(sourceLinkUrl);
            }

            return sourceLinkUrls.OrderBy(_ => _).FirstOrDefault();
        }

        private string? TryGetSourceLinkUrl(DocumentHandle handle)
        {
            var document = _pdbReader.GetDocument(handle);
            try
            {
                if (document.Name.IsNil)
                    return null;
            }
            catch (BadImageFormatException)
            {
                return null;
            }

            var documentName = _pdbReader.GetString(document.Name);
            if (documentName is null)
                return null;

            foreach (var cdiHandle in _pdbReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition))
            {
                var cdi = _pdbReader.GetCustomDebugInformation(cdiHandle);
                if (_pdbReader.GetGuid(cdi.Kind) == PortableCustomDebugInfoKinds.SourceLink && !cdi.Value.IsNil)
                {
                    var blobReader = _pdbReader.GetBlobReader(cdi.Value);
                    var sourceLinkJson = blobReader.ReadUTF8(blobReader.Length);

                    var map = SourceLinkMap.Parse(sourceLinkJson);

                    if (map.TryGetUri(documentName, out var uri))
                    {
                        return uri;
                    }
                }
            }

            return null;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _peReader.Dispose();
            _pdbReaderProvider.Dispose();
        }
    }
}
