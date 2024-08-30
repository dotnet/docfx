// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.Json;
using Docfx.Common;
using Docfx.Plugins;
using YamlDotNet.Serialization;

namespace Docfx.Build.ApiPage;

class ApiPageDocumentProcessor(IMarkdownService markdownService) : IDocumentProcessor
{
    IEnumerable<IDocumentBuildStep> IDocumentProcessor.BuildSteps => Array.Empty<IDocumentBuildStep>();
    void IDocumentProcessor.UpdateHref(FileModel model, IDocumentBuildContext context) { }

    string IDocumentProcessor.Name => nameof(ApiPageDocumentProcessor);

    public ProcessingPriority GetProcessingPriority(FileAndType file)
    {
        if (file.Type != DocumentType.Article)
        {
            return ProcessingPriority.NotSupported;
        }

        var extension = Path.GetExtension(file.File);
        if (".yml".Equals(extension, StringComparison.OrdinalIgnoreCase) ||
            ".yaml".Equals(extension, StringComparison.OrdinalIgnoreCase))
        {
            return YamlMime.ReadMime(file.File) == "YamlMime:ApiPage" ? ProcessingPriority.High : ProcessingPriority.NotSupported;
        }

        return ProcessingPriority.NotSupported;
    }

    public FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
    {
        var deserializer = new DeserializerBuilder().WithAttemptingUnquotedStringTypeDeserialization().Build();
        var yml = EnvironmentContext.FileAbstractLayer.ReadAllText(file.File);
        var json = JsonSerializer.Serialize(deserializer.Deserialize<object>(yml));
        var data = JsonSerializer.Deserialize<ApiPage>(json, ApiPage.JsonSerializerOptions);
        var content = new Dictionary<string, object>(metadata.OrderBy(item => item.Key));

        if (data.metadata is not null)
        {
            foreach (var (key, value) in data.metadata.OrderBy(item => item.Key))
                content[key] = value.Value;
        }

        content["title"] = data.title;
        content["content"] = ApiPageHtmlTemplate.Render(data, Markup).ToString();
        content["yamlmime"] = "ApiPage";
        content["_disableNextArticle"] = true;

        var localPathFromRoot = PathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, EnvironmentContext.FileAbstractLayer.GetPhysicalPath(file.File));

        return new FileModel(file, content)
        {
            DocumentType = "ApiPage",
            LocalPathFromRoot = localPathFromRoot,
        };

        string Markup(string markdown) => markdownService.Markup(markdown, file.File).Html;
    }

    public SaveResult Save(FileModel model)
    {
        return new SaveResult
        {
            DocumentType = model.DocumentType,
            FileWithoutExtension = Path.ChangeExtension(model.File, null),
        };
    }
}
