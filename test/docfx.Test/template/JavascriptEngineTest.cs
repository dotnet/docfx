// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class JavascriptEngineTest
    {
        private readonly JavascriptEngine _js = new JavascriptEngine("data/javascript");

        [Theory]
        [InlineData("{'a':'hello','tags':[1,2],'page':{'value':3}}", "{'a':'hello','tags':[1,2],'page':{'value':3}}")]
        public void RunJavascript(string input, string output)
        {
            var inputJson = JObject.Parse(input.Replace('\'', '"'));
            var outputJson = _js.Run("index.js", inputJson);
            Assert.Equal(output.Replace('\'', '"'), outputJson.ToString(Formatting.None));
        }

        [Theory]
        [InlineData("{'error': true}", "TypeError: a is undefined | fail | index.js")]
        public void RunJavascriptError(string input, string errors)
        {
            var inputJson = JObject.Parse(input.Replace('\'', '"'));
            var exception = Assert.ThrowsAny<Exception>(() => _js.Run("index.js", inputJson));

            foreach (var error in errors.Split('|'))
            {
                Assert.Contains(error.Trim(), exception.ToString());
            }
        }
    }
}
