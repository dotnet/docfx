// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;

using Docfx.Build.Common;
using Docfx.DataContracts.RestApi;
using Docfx.Plugins;

namespace Docfx.Build.RestApi;

[Export(nameof(RestApiDocumentProcessor), typeof(IDocumentBuildStep))]
public class BuildRestApiDocument : BuildReferenceDocumentBase
{
    public override string Name => nameof(BuildRestApiDocument);

    protected override void BuildArticle(IHostService host, FileModel model)
    {
        var restApi = (RestApiRootItemViewModel)model.Content;
        BuildItem(host, restApi, model);
        if (restApi.Children != null)
        {
            foreach (var item in restApi.Children)
            {
                BuildItem(host, item, model);
            }
        }
        if (restApi.Tags != null)
        {
            foreach (var tag in restApi.Tags)
            {
                BuildTag(host, tag, model);
            }
        }
    }

    public static RestApiItemViewModelBase BuildItem(IHostService host, RestApiItemViewModelBase item, FileModel model, Func<string, bool> filter = null)
    {
        item.Summary = Markup(host, item.Summary, model, filter);
        item.Description = Markup(host, item.Description, model, filter);
        if (model.Type != DocumentType.Overwrite)
        {
            item.Conceptual = Markup(host, item.Conceptual, model, filter);
            item.Remarks = Markup(host, item.Remarks, model, filter);
        }

        if (item is RestApiRootItemViewModel root)
        {
            if (root.Info != null) root.Info.Description = Markup(host, root.Info.Description, model, filter);
            if (root.ExternalDocs != null) root.ExternalDocs.Description = Markup(host, root.ExternalDocs.Description, model, filter);
            foreach (var security in root.SecurityDefinitions?.Values.AsEnumerable() ?? [])
            {
                if (security != null) security.Description = Markup(host, security.Description, model, filter);
            }
            MarkupServers(root.Servers);
            foreach (var schema in root.Schemas?.Values.AsEnumerable() ?? []) MarkupSchema(schema);
        }
        if (item is RestApiChildItemViewModel child)
        {
            MarkupServers(child.Servers);
            if (child.RequestBody is { } body)
            {
                body.Description = Markup(host, body.Description, model, filter);
                MarkupContent(body.Content);
            }
            foreach (var parameter in child.Parameters ?? [])
            {
                parameter.Description = Markup(host, parameter.Description, model, filter);
                MarkupSchema(parameter.Schema);
                MarkupContent(parameter.Content);
            }
            foreach (var response in child.Responses ?? [])
            {
                response.Description = Markup(host, response.Description, model, filter);
                MarkupSchema(response.Schema);
                MarkupContent(response.Content);
                foreach (var header in response.Headers?.Values.AsEnumerable() ?? []) MarkupSchema(header);
            }
        }
        return item;

        void MarkupServers(List<RestApiServerViewModel> servers)
        {
            foreach (var server in servers ?? [])
            {
                if (server != null) server.Description = Markup(host, server.Description, model, filter);
            }
        }

        void MarkupContent(List<RestApiMediaTypeViewModel> content)
        {
            foreach (var media in content ?? [])
            {
                if (media == null) continue; // Positional overwrite placeholder.
                MarkupSchema(media.Schema);
                MarkupSchema(media.ItemSchema);
            }
        }

        void MarkupSchema(RestApiSchemaViewModel schema)
        {
            if (schema == null) return;
            schema.Description = Markup(host, schema.Description, model, filter);
            foreach (var property in schema.Properties?.Values.AsEnumerable() ?? []) MarkupSchema(property);
            MarkupSchema(schema.Items);
            foreach (var branch in schema.AllOf ?? []) MarkupSchema(branch);
            foreach (var composition in schema.Composition ?? [])
            {
                if (composition == null) continue;
                foreach (var branch in composition.Schemas ?? []) MarkupSchema(branch);
            }
        }
    }

    public static RestApiTagViewModel BuildTag(IHostService host, RestApiTagViewModel tag, FileModel model, Func<string, bool> filter = null)
    {
        tag.Conceptual = Markup(host, tag.Conceptual, model, filter);
        tag.Description = Markup(host, tag.Description, model, filter);
        return tag;
    }

    private static string Markup(IHostService host, string markdown, FileModel model, Func<string, bool> filter = null)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return markdown;
        }

        if (filter != null && filter(markdown))
        {
            return markdown;
        }

        var mr = host.Markup(markdown, model.OriginalFileAndType);
        model.LinkToFiles = model.LinkToFiles.Union(mr.LinkToFiles);
        model.LinkToUids = model.LinkToUids.Union(mr.LinkToUids);

        var fls = model.FileLinkSources.ToDictionary(p => p.Key, p => p.Value);
        foreach (var pair in mr.FileLinkSources)
        {
            if (fls.TryGetValue(pair.Key, out ImmutableList<LinkSourceInfo> list))
            {
                fls[pair.Key] = list.AddRange(pair.Value);
            }
            else
            {
                fls[pair.Key] = pair.Value;
            }
        }
        model.FileLinkSources = fls.ToImmutableDictionary();

        var uls = model.UidLinkSources.ToDictionary(p => p.Key, p => p.Value);
        foreach (var pair in mr.UidLinkSources)
        {
            if (uls.TryGetValue(pair.Key, out ImmutableList<LinkSourceInfo> list))
            {
                uls[pair.Key] = list.AddRange(pair.Value);
            }
            else
            {
                uls[pair.Key] = pair.Value;
            }
        }
        model.UidLinkSources = uls.ToImmutableDictionary();

        return mr.Html;
    }
}
