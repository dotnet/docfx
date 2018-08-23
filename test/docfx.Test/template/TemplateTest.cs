// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class TemplateTest
    {
        [Theory]
        [InlineData(typeof(Conceptual), "{'html':'hello'}", "hello")]
        public async Task RenderTemplate(Type pageType, string json, string html)
        {
            var model = JsonConvert.DeserializeObject(json.Replace('\'', '"'), pageType);
            Assert.Equal(html, await Template.Render(pageType.Name, model));
        }
    }
}
