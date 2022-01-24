// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;
using Xunit;
using Yunit;

namespace Microsoft.Docs.Build;

public class MustacheTemplateTest
{
    private readonly MustacheTemplate _template = new(new LocalPackage("data/mustache"));

    [Theory]
    [InlineData("section", "{'section':{'value':'value','foo':'foo'}}", "<p>value</p>")]
    [InlineData("section", "{'value': 'abc', 'section':{'value':null}}", "<p></p>")]
    [InlineData("section", "{'value': 'abc', 'label':{'value':null}}", "")]
    [InlineData("section", "{'section':'false'}", "<p></p>")]
    [InlineData("section", "{'section':'true'}", "<p></p>")]
    [InlineData("section", "{'section':' '}", "<p></p>")]
    [InlineData("section", "{'section':false}", "")]
    [InlineData("section", "{'section':true}", "<p></p>")]
    [InlineData("section", "{'section':{}}", "<p></p>")]
    [InlineData("section", "{'section':[]}", "")]
    [InlineData("section", "{'section':[{'value':1}, {'value':2}]}", "<p>1</p><p>2</p>")]
    [InlineData("section", "{'section':null}", "")]
    [InlineData("section", "{'section':0}", "")]
    [InlineData("section", "{'section':1}", "<p></p>")]
    [InlineData("section", "{'section':'string'}", "<p></p>")]
    [InlineData("section", "{'section':''}", "")]
    [InlineData("section", "{'me':'a'}", "a me")]
    [InlineData(
        "include",
        "{'description':'hello','tags':[1,2],'page':{'value':3}}",
        "<div>hello<div>a b<p>1</p><p>2</p></div>3</div>")]
    [InlineData(
        "list",
        "{'list':['l1','l2'], 'list-empty':[], 'list-empty-string':['']}",
        "list=' l1 l2'")]
    [InlineData(
        "list",
        "{'parent': [{'child': [1,2]}, {'child': ''}]}",
        "1")]
    public void RenderMustacheTemplate(string name, string json, string html)
    {
        var model = JToken.Parse(json.Replace('\'', '"'));

        Assert.Equal(
            JsonDiff.NormalizeHtml(html).Replace('\'', '"'),
            JsonDiff.NormalizeHtml(_template.Render(ErrorBuilder.Null, name, model)));
    }
}
