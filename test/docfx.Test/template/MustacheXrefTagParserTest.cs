// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build;

public class MustacheXrefTagParserTest
{
    [Theory]
    [InlineData("<no xref>", "<no xref>")]
    [InlineData(
        "<xref/>",
        "{{#uid.__xrefspec}}" +
        "  {{#href}}" +
        "    <a href='{{href}}'> {{#name}}{{.}}{{/name}}{{^name}}{{uid}}{{/name}} </a>" +
        "  {{/href}}" +
        "  {{^href}}" +
        "    <span> {{#name}}{{.}}{{/name}}{{^name}}{{uid}}{{/name}} </span>" +
        "  {{/href}}" +
        "{{/uid.__xrefspec}}")]
    [InlineData(
        "<xref uid='{{uid}}'/>",
        "{{#uid.__xrefspec}}" +
        "  {{#href}}" +
        "    <a href='{{href}}'> {{#name}}{{.}}{{/name}}{{^name}}{{uid}}{{/name}} </a>" +
        "  {{/href}}" +
        "  {{^href}}" +
        "    <span> {{#name}}{{.}}{{/name}}{{^name}}{{uid}}{{/name}} </span>" +
        "  {{/href}}" +
        "{{/uid.__xrefspec}}")]
    [InlineData(
        "<xref uid='{{namespace}}'/>",
        "{{#namespace.__xrefspec}}" +
        "  {{#href}}" +
        "    <a href='{{href}}'> {{#name}}{{.}}{{/name}}{{^name}}{{uid}}{{/name}} </a>" +
        "  {{/href}}" +
        "  {{^href}}" +
        "    <span> {{#name}}{{.}}{{/name}}{{^name}}{{uid}}{{/name}} </span>" +
        "  {{/href}}" +
        "{{/namespace.__xrefspec}}")]
    [InlineData(
        "<xref uid='{{ uid }}' template='partials/dotnet/xref-name.tmpl' />",
        "{{#uid.__xrefspec}}" +
        "  {{#href}}" +
        "    {{> partials/dotnet/xref-name.tmpl}}" +
        "  {{/href}}" +
        "  {{^href}}" +
        "    <span> {{#name}}{{.}}{{/name}}{{^name}}{{uid}}{{/name}} </span>" +
        "  {{/href}}" +
        "{{/uid.__xrefspec}}")]
    [InlineData(
        "<xref uid='{{ . }}'/>",
        "{{#..__xrefspec}}" +
        "{{#href}}" +
        "  <a href='{{href}}'> {{#name}}{{.}}{{/name}}{{^name}}{{uid}}{{/name}} </a>" +
        "{{/href}}" +
        "{{^href}}" +
        "  <span> {{#name}}{{.}}{{/name}}{{^name}}{{uid}}{{/name}} </span>" +
        "{{/href}}" +
        "{{/..__xrefspec}}")]
    [InlineData(
        "<xref uid='{{ . }}' title='{{title}}'/>",
        "{{#..__xrefspec}}" +
        "{{#href}}" +
        "  <a href='{{href}}' title='{{title}}'> {{#name}}{{.}}{{/name}}{{^name}}{{uid}}{{/name}} </a>" +
        "{{/href}}" +
        "{{^href}}" +
        "  <span> {{#name}}{{.}}{{/name}}{{^name}}{{uid}}{{/name}} </span>" +
        "{{/href}}" +
        "{{/..__xrefspec}}")]
    [InlineData(
        "<xref href='{{uid-from-href}}' title='{{name}}'>" +
        "  <h3>{{name}}</h3>" +
        "</xref>",
        "{{#uid-from-href.__xrefspec}}" +
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
        "{{/uid-from-href.__xrefspec}}")]
    [InlineData(
        "<xref uid='{{uid-higher-priority}}' href='{{uid}}' title='{{name}}'>" +
        "  <h3>{{name}}</h3>" +
        "</xref>",
        "{{#uid-higher-priority.__xrefspec}}" +
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
        "{{/uid-higher-priority.__xrefspec}}")]
    [InlineData(
        "<xref uid='{{url}}'></xref>",
        "{{#url.__xrefspec}}" +
        "  {{#href}}" +
        "    <a href='{{href}}'>" +
        "  {{/href}}" +
        "  {{^href}}" +
        "    <span>" +
        "  {{/href}}" +
        "  {{#name}}{{.}}{{/name}}{{^name}}{{uid}}{{/name}}" +
        "  {{#href}}" +
        "    </a>" +
        "  {{/href}}" +
        "  {{^href}}" +
        "    </span>" +
        "  {{/href}}" +
        "{{/url.__xrefspec}}")]
    public void ProcessXrefTag(string template, string expected)
    {
        template = template.Replace('\'', '"');
        Assert.Equal(
        expected.Replace('\'', '"').Replace(" ", ""),
        MustacheXrefTagParser.ProcessXrefTag(template).Replace(" ", ""));
    }
}
