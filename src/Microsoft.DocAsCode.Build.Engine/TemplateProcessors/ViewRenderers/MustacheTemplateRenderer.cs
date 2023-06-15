// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Stubble.Core.Builders;
using Stubble.Core.Interfaces;
using System.Text.RegularExpressions;

namespace Microsoft.DocAsCode.Build.Engine;

internal class MustacheTemplateRenderer : ITemplateRenderer
{
    public const string Extension = ".tmpl";

    private static readonly Regex IncludeRegex = new(@"{{\s*!\s*include\s*\(:?(:?['""]?)\s*(?<file>(.+?))\1\s*\)\s*}}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MasterPageRegex = new(@"{{\s*!\s*master\s*\(:?(:?['""]?)\s*(?<file>(.+?))\1\s*\)\s*}}\s*\n?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MasterPageBodyRegex = new(@"{{\s*!\s*body\s*}}\s*\n?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IResourceFileReader _reader;
    private readonly IStubbleRenderer _renderer;
    private readonly string _template;

    public MustacheTemplateRenderer(IResourceFileReader reader, ResourceInfo info, string name = null)
    {
        if (info == null)
        {
            throw new ArgumentNullException(nameof(info));
        }

        if (info.Content == null)
        {
            throw new ArgumentNullException(nameof(info.Content));
        }

        if (info.Path == null)
        {
            throw new ArgumentNullException(nameof(info.Path));
        }

        Path = info.Path;
        Name = name ?? System.IO.Path.GetFileNameWithoutExtension(Path);
        _reader = reader;

        _renderer = new StubbleBuilder()
            .Configure(c =>
            {
                c.SetPartialTemplateLoader(new ResourceTemplateLoader(reader));
                c.AddSectionBlacklistType(typeof(System.Dynamic.IDynamicMetaObjectProvider));
            })
            .Build();

        var processedTemplate = ParseTemplateHelper.ExpandMasterPage(reader, info, MasterPageRegex, MasterPageBodyRegex);

        _template = processedTemplate;

        Dependencies = ExtractDependencyResourceNames(processedTemplate).ToList();
    }

    public IEnumerable<string> Dependencies { get; }

    public string Raw { get; }

    public string Path { get; }

    public string Name { get; }

    public string Render(object model)
    {
        return _renderer.Render(_template, model);
    }

    /// <summary>
    /// Dependent files are defined in following syntax in Mustache template leveraging Mustache Comments
    /// {{! include('file') }}
    /// file path can be wrapped by quote ' or double quote " or none
    /// </summary>
    /// <param name="template"></param>
    private IEnumerable<string> ExtractDependencyResourceNames(string template)
    {
        foreach (Match match in IncludeRegex.Matches(template))
        {
            var filePath = match.Groups["file"].Value;
            foreach (var name in ParseTemplateHelper.GetResourceName(filePath, Path, _reader))
            {
                yield return name;
            }
        }
    }
}
