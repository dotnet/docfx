// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsTest;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class LiquidTemplateTest
    {
        private readonly LiquidTemplate _template = new LiquidTemplate("data/liquid");

        [Theory]
        [InlineData("test", "{'description':'hello','tags':[1,2],'page':{'value':3}}", "<div>hello<div>a b<p>1</p><p>2</p></div>3</div>")]
        public void RenderLiquidTemplate(string name, string json, string html)
        {
            var model = JObject.Parse(json.Replace('\'', '"'));
            var result = _template.Render(name, model);

            Assert.Equal(
                JsonDiff.NormalizeHtml(html),
                JsonDiff.NormalizeHtml(result));
        }
    }
}
