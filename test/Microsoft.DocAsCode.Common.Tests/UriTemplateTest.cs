// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using Xunit;

    [Trait("Owner", "vwxyzh")]
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
        public void TestUriTemplate_PipelineInEnviroment()
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
}
