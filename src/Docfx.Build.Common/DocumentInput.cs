// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Docfx.Common;
using Docfx.Plugins;
using Newtonsoft.Json;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Docfx.Build.Common;

public sealed class DocumentInput
{
    public sealed record DocumentHeader(string Kind, string Version = null);

    private static readonly ConditionalWeakTable<FileAndType, DocumentInput> Inputs = new();
    private readonly Func<TextReader> _open;
    private readonly Lazy<DocumentHeader> _header;
    private readonly Lazy<string> _text;

    private DocumentInput(string path, string format, Func<TextReader> open)
    {
        Path = path;
        Format = format;
        _open = open;
        _header = new(() =>
        {
            if (format == null) return null;
            try
            {
                using var reader = open();
                return ReadHeader(reader, format);
            }
            catch (Exception ex) when (ex is IOException or JsonException or YamlException)
            {
                Logger.LogVerbose($"Could not identify document '{path}': {ex.Message}");
                return null;
            }
        });
        _text = new(() =>
        {
            using var reader = open();
            return reader.ReadToEnd();
        });
    }

    public string Path { get; }
    public string Format { get; }
    public DocumentHeader Header => _header.Value;
    public string ReadAllText() => _text.Value;
    public TextReader OpenRead() => _text.IsValueCreated ? new StringReader(_text.Value) : _open();

    public static DocumentInput Get(FileAndType file) => Inputs.TryGetValue(file, out var input) ? input : Create(file);

    public static DocumentInput FromText(string text, string format) =>
        new(System.IO.Path.GetFullPath("document." + format), format, () => new StringReader(text));

    // Share inputs across processor selection and loading, without retaining open files
    // or reusing stale contents when the same FileCollection is built again.
    public static IDisposable BeginRead(IEnumerable<FileAndType> files) => new InputScope(files.ToArray());

    private static DocumentInput Create(FileAndType file)
    {
        var path = System.IO.Path.Combine(file.BaseDir, file.File);
        var format = System.IO.Path.GetExtension(file.File).ToLowerInvariant() switch
        {
            ".json" => "json",
            ".yaml" or ".yml" or ".csyaml" or ".csyml" => "yaml",
            _ => null
        };
        return new(path, format, () => EnvironmentContext.FileAbstractLayer.OpenReadText(path));
    }

    private sealed class InputScope : IDisposable
    {
        private readonly FileAndType[] _files;

        public InputScope(FileAndType[] files)
        {
            _files = files;
            foreach (var file in files) Inputs.Add(file, Create(file));
        }

        public void Dispose()
        {
            foreach (var file in _files) Inputs.Remove(file);
        }
    }

    public static DocumentHeader ReadHeader(TextReader source, string format)
    {
        if (format == "json")
        {
            using var reader = new JsonTextReader(source) { DateParseHandling = DateParseHandling.None, CloseInput = false };
            if (!reader.Read() || reader.TokenType != JsonToken.StartObject) return null;
            DocumentHeader swagger = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject && reader.Depth == 0) return swagger;
                if (reader.TokenType != JsonToken.PropertyName || reader.Depth != 1) continue;
                var key = (string)reader.Value;
                if (!reader.Read()) return null;
                if (reader.TokenType == JsonToken.String)
                {
                    if (key == "openapi") return new(key, (string)reader.Value);
                    // Retain Swagger's existing ownership rule: malformed JSON is not claimed.
                    if (key == "swagger") swagger = new(key, (string)reader.Value);
                }
                reader.Skip();
            }
            return null;
        }
        if (format != "yaml") return null;
        // A leading YamlMime comment identifies the document even if its body is invalid.
        if (source.Peek() == '#' && YamlMime.ReadMime(source) is { } mime) return new(mime);
        var parser = new Parser(source);
        parser.Consume<StreamStart>();
        if (!parser.TryConsume<DocumentStart>(out _) || !parser.TryConsume<MappingStart>(out _)) return null;
        while (!parser.Accept<MappingEnd>(out _))
        {
            if (!parser.TryConsume<Scalar>(out var key)) return null;
            if (key.Value is "openapi" or "swagger" && parser.TryConsume<Scalar>(out var version)) return new(key.Value, version.Value);
            parser.SkipThisAndNestedEvents();
        }
        return null;
    }
}
