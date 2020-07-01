// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build
{
    public class MustacheXrefTagParserTest
    {
        [Theory]
        [InlineData("<no xref>", "<no xref>")]
        [InlineData("<xref/>",
            "{{#uid}}" +
            "  {{#href}}" +
            "    <a href='{{href}}'> {{name}} </a>" +
            "  {{/href}}" +
            "  {{^href}}" +
            "    <span> {{name}} </span>" +
            "  {{/href}}" +
            "{{/uid}}"
            )]
        [InlineData("<xref uid='{{uid}}'/>",
            "{{#uid}}" +
            "  {{#href}}" +
            "    <a href='{{href}}'> {{name}} </a>" +
            "  {{/href}}" +
            "  {{^href}}" +
            "    <span> {{name}} </span>" +
            "  {{/href}}" +
            "{{/uid}}"
            )]
        [InlineData("<xref uid='{{namespace}}'/>",
            "{{#namespace}}" +
            "  {{#href}}" +
            "    <a href='{{href}}'> {{name}} </a>" +
            "  {{/href}}" +
            "  {{^href}}" +
            "    <span> {{name}} </span>" +
            "  {{/href}}" +
            "{{/namespace}}"
            )]
        [InlineData("<xref uid='{{ uid }}' template='partials/dotnet/xref-name.tmpl' />",
            "{{#uid}}" +
            "  {{#href}}" +
            "    {{> partials/dotnet/xref-name.tmpl}}" +
            "  {{/href}}" +
            "  {{^href}}" +
            "    <span> {{name}} </span>" +
            "  {{/href}}" +
            "{{/uid}}"
            )]
        [InlineData("<xref uid='{{ . }}'/>",
            "{{#.}}" +
            "{{#href}}" +
            "  <a href='{{href}}'> {{name}} </a>" +
            "{{/href}}" +
            "{{^href}}" +
            "  <span> {{name}} </span>" +
            "{{/href}}" +
            "{{/.}}"
            )]
        [InlineData("<xref uid='{{ . }}' title='{{title}}'/>",
            "{{#.}}" +
            "{{#href}}" +
            "  <a href='{{href}}' title='{{title}}'> {{name}} </a>" +
            "{{/href}}" +
            "{{^href}}" +
            "  <span> {{name}} </span>" +
            "{{/href}}" +
            "{{/.}}"
            )]
        [InlineData(
            "<xref href='{{uid-from-href}}' title='{{name}}'>" +
            "  <h3>{{name}}</h3>" +
            "</xref>",
            "{{#uid-from-href}}" +
            "  {{#href}}" +
            "    <a href='{{href}}' title='{{name}}'>" +
            "  {{/href}}" +
            "  {{^href}}" +
            "    <span>" +
            "  {{/href}}" +
            "  <h3>{{name}}</h3>" +
            "  {{#href}}" +
            "    </a>" +
            "  {{/href}}" +
            "  {{^href}}" +
            "    </span>" +
            "  {{/href}}" +
            "{{/uid-from-href}}"
            )]
        [InlineData(
            "<xref uid='{{uid-higher-priority}}' href='{{uid}}' title='{{name}}'>" +
            "  <h3>{{name}}</h3>" +
            "</xref>",
            "{{#uid-higher-priority}}" +
            "  {{#href}}" +
            "    <a href='{{href}}' title='{{name}}'>" +
            "  {{/href}}" +
            "  {{^href}}" +
            "    <span>" +
            "  {{/href}}" +
            "  <h3>{{name}}</h3>" +
            "  {{#href}}" +
            "    </a>" +
            "  {{/href}}" +
            "  {{^href}}" +
            "    </span>" +
            "  {{/href}}" +
            "{{/uid-higher-priority}}"
            )]
        [InlineData(
            "<xref uid='{{url}}'></xref>",
            "{{#url}}" +
            "  {{#href}}" +
            "    <a href='{{href}}'>" +
            "  {{/href}}" +
            "  {{^href}}" +
            "    <span>" +
            "  {{/href}}" +
            "  {{name}}" +
            "  {{#href}}" +
            "    </a>" +
            "  {{/href}}" +
            "  {{^href}}" +
            "    </span>" +
            "  {{/href}}" +
            "{{/url}}"
            )]
        public void ProcessXrefTag(string template, string expected)
        {
            template = template.Replace('\'', '"');
            Assert.Equal(
            expected.Replace('\'', '"').Replace(" ", ""),
            MustacheXrefTagParser.ProcessXrefTag(template).Replace(" ", ""));
        }
    }
}
