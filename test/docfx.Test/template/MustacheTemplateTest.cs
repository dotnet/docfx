// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class MustacheTemplateTest
    {
        private readonly MustacheTemplate _template = new MustacheTemplate("data/mustache");

        [Theory]
        [InlineData("section.tmpl", "{'section':{'value':'value','foo':'foo'}}", "<p>value</p>")]
        [InlineData("section.tmpl", "{'value': 'abc', 'section':{'value':null}}", "<p></p>")]
        [InlineData("section.tmpl", "{'value': 'abc', 'label':{'value':null}}", "")]
        [InlineData("section.tmpl", "{'section':false}", "")]
        [InlineData("section.tmpl", "{'section':true}", "<p></p>")]
        [InlineData("section.tmpl", "{'section':{}}", "<p></p>")]
        [InlineData("section.tmpl", "{'section':[]}", "")]
        [InlineData("section.tmpl", "{'section':[{'value':1}, {'value':2}]}", "<p>1</p><p>2</p>")]
        [InlineData("section.tmpl", "{'section':null}", "")]
        [InlineData("section.tmpl", "{'section':0}", "")]
        [InlineData("section.tmpl", "{'section':1}", "<p></p>")]
        [InlineData("section.tmpl", "{'section':'string'}", "<p></p>")]
        [InlineData("section.tmpl", "{'section':''}", "")]
        [InlineData(
            "include.tmpl",
            "{'description':'hello','tags':[1,2],'page':{'value':3}}",
            "<div>hello<div>a b<p>1</p><p>2</p></div>3</div>")]
        public void RenderMustacheTemplate(string name, string json, string html)
        {
            var model = JToken.Parse(json.Replace('\'', '"'));

            Assert.Equal(
                TestUtility.NormalizeHtml(html).Replace('\'', '"'),
                TestUtility.NormalizeHtml(_template.Render(name, model)));
        }
    }
}
