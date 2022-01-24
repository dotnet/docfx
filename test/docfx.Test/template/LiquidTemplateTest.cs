// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;
using Xunit;
using Yunit;

namespace Microsoft.Docs.Build;

public class LiquidTemplateTest
{
    private readonly LiquidTemplate _template = new(new LocalPackage("data/liquid"));

    [Theory]
    [InlineData("test", "{'description':'hello','tags':[1,2],'page':{'value':3}}", "<div>hello<div>a b<p>1</p><p>2</p></div>3</div>")]
    [InlineData("test", "{'description':'hello-1','Description':'hello-2','tags':[1,2],'page':{'value':3}}", "<div>hello-2<div>a b<p>1</p><p>2</p></div>3</div>")]
    [InlineData("test", "{'description':'hello','tags':[1,2],'page':{'value':3, 'Value':4}}", "<div>hello<div>a b<p>1</p><p>2</p></div>4</div>")]
    public void RenderLiquidTemplate(string name, string json, string html)
    {
        var model = JObject.Parse(json.Replace('\'', '"'));
        var result = _template.Render(ErrorBuilder.Null, name, new SourceInfo<string>(name), model);

        Assert.Equal(
            JsonDiff.NormalizeHtml(html),
            JsonDiff.NormalizeHtml(result));
    }
}
