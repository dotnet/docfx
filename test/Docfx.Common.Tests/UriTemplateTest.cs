// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Docfx.Plugins;

using Xunit;

namespace Docfx.Common.Tests;

[Trait("Related", "UriTemplate")]
public class UriTemplateTest
{
    [Fact]
    public void TestUriTemplate_Variable()
    {
        var template = UriTemplate<string>.Parse("*{var}*", s => s, s => null);
        var actual = template.Evaluate(
            new Dictionary<string, string>
            {
                ["var"] = "--"
            });
        Assert.Equal("*--*", actual);
    }

    [Fact]
    public void TestUriTemplate_EnvironmentVariable()
    {
        Environment.SetEnvironmentVariable("var", "@{var}@");
        var template = UriTemplate<string>.Parse("*{%var%}*", s => s, s => null);
        Environment.SetEnvironmentVariable("var", null);
        var actual = template.Evaluate(
            new Dictionary<string, string>
            {
                ["var"] = "--"
            });
        Assert.Equal("*@--@*", actual);
    }

    [Fact]
    public void TestUriTemplate_SimplePipeline()
    {
        var template = UriTemplate<string>.Parse(
            " a |>trim",
            s => s,
            CreatePipeline);
        var actual = template.Evaluate(new Dictionary<string, string>());
        Assert.Equal("a", actual);
    }

    [Fact]
    public void TestUriTemplate_Pipeline_WithParameters()
    {
        var template = UriTemplate<string>.Parse(
            "a bc d |>warpWord < >",
            s => s,
            CreatePipeline);
        var actual = template.Evaluate(new Dictionary<string, string>());
        Assert.Equal("<a> <bc> <d> ", actual);
    }

    [Fact]
    public void TestUriTemplate_Multipipeline()
    {
        var template = UriTemplate<string>.Parse(
            " a bc d |>trim|>warpWord < >",
            s => s,
            CreatePipeline);
        var actual = template.Evaluate(new Dictionary<string, string>());
        Assert.Equal("<a> <bc> <d>", actual);
    }

    [Fact]
    public void TestUriTemplate_PipelineInEnvironment()
    {
        Environment.SetEnvironmentVariable("pipeline", "|>trim|>warpWord < >");
        var template = UriTemplate<string>.Parse(
            " a bc d {%pipeline%}",
            s => s,
            CreatePipeline);
        Environment.SetEnvironmentVariable("pipeline", null);
        var actual = template.Evaluate(new Dictionary<string, string>());
        Assert.Equal("<a> <bc> <d>", actual);
    }

    [Fact]
    public async Task TestUriTemplate_Task()
    {
        var template = UriTemplate<Task<string[]>>.Parse(
            "a bc d |>warpWord < >",
            s => Task.FromResult(s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)),
            s => new WrapWordPipeline());
        var actual = template.Evaluate(new Dictionary<string, string>());
        Assert.Equal(new[] { "<a>", "<bc>", "<d>" }, await actual);
    }

    private IUriTemplatePipeline<string> CreatePipeline(string name)
    {
        switch (name)
        {
            case "trim":
                return new TrimPipeline();
            case "warpWord":
                return new WrapWordPipeline();
            default:
                throw new NotSupportedException();
        }
    }

    private sealed class TrimPipeline : IUriTemplatePipeline<string>
    {
        public string Handle(string value, string[] parameters) => value.Trim();
    }

    private sealed class WrapWordPipeline
        : IUriTemplatePipeline<string>, IUriTemplatePipeline<Task<string[]>>
    {
        public string Handle(string value, string[] parameters) =>
            Regex.Replace(value, @"\w+", m => parameters[0] + m.Value + parameters[1]);

        public async Task<string[]> Handle(Task<string[]> value, string[] parameters) =>
            Array.ConvertAll(await value, v => parameters[0] + v + parameters[1]);
    }
}
