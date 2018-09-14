// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class RazorTemplateTest
    {
        [Theory]
        [InlineData(typeof(TestPage), "{'description':'hello'}", "<div>{&quot;Description&quot;:&quot;hello&quot;}</div>")]
        public async Task RenderRazorTemplate(Type pageType, string json, string html)
        {
            var model = JsonConvert.DeserializeObject(json.Replace('\'', '"'), pageType);

            Assert.Equal(
                TestUtility.NormalizeHtml(html),
                TestUtility.NormalizeHtml(await RazorTemplate.Render(pageType.Name, model)));
        }
    }
}
