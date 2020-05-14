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
            "  {{#href}}" +
            "    <a href=\"{{href}}\"> {{name}} </a>" +
            "  {{/href}}" +
            "  {{^href}}" +
            "    <span> {{name}} </span>" +
            "  {{/href}}" +
            "{{/uid}}"
            )]
        [InlineData("<xref uid='{{uid}}'/>",
            "{{#uid}}" +
            "  {{#href}}" +
            "    <a href=\"{{href}}\"> {{name}} </a>" +
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
            "{{#href}}" +
            "  <a href=\"{{href}}\"> {{name}} </a>" +
            "{{/href}}" +
            "{{^href}}" +
            "  <span> {{name}} </span>" +
            "{{/href}}"
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
