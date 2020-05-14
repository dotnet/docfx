// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build
{
    public class MustacheXrefTagParserTest
    {
        [Theory]
        [InlineData("<xref/>",
            "{{#uid}}" +
            "  {{#resolved}}" +
            "    <a href=\"{{href}}\"> {{name}} </a>" +
            "  {{/resolved}}" +
            "  {{^resolved}}" +
            "    <span> {{uid}} </span>" +
            "  {{/resolved}}" +
            "{{/uid}}"
            )]
        [InlineData("<xref uid='{{uid}}'/>",
            "{{#uid}}" +
            "  {{#resolved}}" +
            "    <a href=\"{{href}}\"> {{name}} </a>" +
            "  {{/resolved}}" +
            "  {{^resolved}}" +
            "    <span> {{uid}} </span>" +
            "  {{/resolved}}" +
            "{{/uid}}"
            )]
        [InlineData("<xref uid='{{namespace}}'/>",
            "{{#namespace}}" +
            "  {{#resolved}}" +
            "    <a href='{{href}}'> {{name}} </a>" +
            "  {{/resolved}}" +
            "  {{^resolved}}" +
            "    <span> {{uid}} </span>" +
            "  {{/resolved}}" +
            "{{/namespace}}"
            )]
        [InlineData("<xref uid='{{ uid }}' template='partials/dotnet/xref-name.tmpl' />",
            "{{#uid}}" +
            "  {{#resolved}}" +
            "    {{> partials/dotnet/xref-name.tmpl}}" +
            "  {{/resolved}}" +
            "  {{^resolved}}" +
            "    <span> {{uid}} </span>" +
            "  {{/resolved}}" +
            "{{/uid}}"
            )]
        [InlineData("<xref uid='{{ . }}'/>",
            "{{#resolved}}" +
            "  <a href=\"{{href}}\"> {{name}} </a>" +
            "{{/resolved}}" +
            "{{^resolved}}" +
            "  <span> {{uid}} </span>" +
            "{{/resolved}}"
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
