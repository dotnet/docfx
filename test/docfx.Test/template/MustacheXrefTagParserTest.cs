// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class MustacheXrefTagParserTest
    {
        [Theory]
        [InlineData("<xref uid='{{uid}}'/>",
            "{{#uid}}" +
            "  {{#href}}" +
            "    <a href=\"{{href}}\"> {{uid}} </a>" +
            "  {{/href}}" +
            "  {{^href}}" +
            "    <span> {{.}} </span>" +
            "  {{/href}}" +
            "{{/uid}}"
            )]
        [InlineData("<xref uid='{{namespace}}'/>",
            "{{#namespace}}" +
            "  {{#href}}" +
            "    <a href='{{href}}'> {{uid}} </a>" +
            "  {{/href}}" +
            "  {{^href}}" +
            "    <span> {{.}} </span>" +
            "  {{/href}}" +
            "{{/namespace}}"
            )]
        [InlineData("<xref uid='{{ uid }}' template='partials/dotnet/xref-name.tmpl' />",
            "{{#uid}}" +
            "  {{#href}}" +
            "    {{> partials/dotnet/xref-name.tmpl}}" +
            "  {{/href}}" +
            "  {{^href}}" +
            "    <span> {{.}} </span>" +
            "  {{/href}}" +
            "{{/uid}}"
            )]
        [InlineData("<xref uid='{{ . }}'/>",
            "{{#href}}" +
            "  <a href=\"{{href}}\"> {{uid}} </a>" +
            "{{/href}}" +
            "{{^href}}" +
            "  <span> {{.}} </span>" +
            "{{/href}}"
            )]
        [InlineData("<xref uid='{{ . }}' template='partials/module/xref-unit-link.tmpl' />",
            "{{#href}}" +
            "  {{> partials/module/xref-unit-link.tmpl}}" +
            "{{/href}}" +
            "{{^href}}" +
            "  <span> {{.}} </span>" +
            "{{/href}}"
            )]
        [InlineData("<xref no-uid='{{ . }}'/>", null)]
        public void ProcessXrefTag(string template, string expected)
        {
            template = template.Replace('\'', '"');
            if (expected != null)
            {
                Assert.Equal(
                expected.Replace('\'', '"').Replace(" ", ""),
                MustacheXrefTagParser.ProcessXrefTag("template-file", template).Replace(" ", ""));
            }
            else
            {
                Assert.Throws<NotSupportedException>(() => MustacheXrefTagParser.ProcessXrefTag("template-file", template));
            }
            
        }
    }
}
