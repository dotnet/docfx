// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Stubble.Core.Builders;
using Stubble.Core.Interfaces;

namespace Docfx.Build.Engine;

internal partial class MustacheTemplateRenderer : ITemplateRenderer
{
    public const string Extension = ".tmpl";


    // Following regex are not supported by GeneratedRegexAttribute. (Because it contains case-insensitive backreferences)
    // When using GeneratedRegexAttribute. following message are shown as information leve.
    //  SYSLIB1044: The regex generator couldn't generate a complete source implementation for the specified regular expression due to an internal limitation. See the explanation in the generated source for more details.
    // And Regex instance is created with following remarks comments.
    //   A custom Regex-derived type could not be generated because the expression contains case-insensitive backreferences which are not supported by the source generator.
#pragma warning disable SYSLIB1045
    private static readonly Regex IncludeRegex = new(@"{{\s*!\s*include\s*\(:?(:?['""]?)\s*(?<file>(.+?))\1\s*\)\s*}}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MasterPageRegex = new(@"{{\s*!\s*master\s*\(:?(:?['""]?)\s*(?<file>(.+?))\1\s*\)\s*}}\s*\n?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
#pragma warning restore SYSLIB1045

    [GeneratedRegex(@"{{\s*!\s*body\s*}}\s*\n?", RegexOptions.IgnoreCase)]
    private static partial Regex MasterPageBodyRegex();

    private readonly ResourceFileReader _reader;
    private readonly IStubbleRenderer _renderer;
    private readonly string _template;

    public MustacheTemplateRenderer(ResourceFileReader reader, ResourceInfo info, string name = null)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(info.Content);
        ArgumentNullException.ThrowIfNull(info.Path);

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

        var processedTemplate = ParseTemplateHelper.ExpandMasterPage(reader, info, MasterPageRegex, MasterPageBodyRegex());

        _template = processedTemplate;

        Dependencies = ExtractDependencyResourceNames(processedTemplate).ToList();
    }

    public IEnumerable<string> Dependencies { get; }

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
