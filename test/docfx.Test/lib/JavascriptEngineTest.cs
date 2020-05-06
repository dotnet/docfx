// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class JavascriptEngineTest
    {
        private readonly IJavaScriptEngine[] _engines = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new IJavaScriptEngine[] { new JintJsEngine("data/javascript"), new ChakraCoreJsEngine("data/javascript") }
            : new IJavaScriptEngine[] { new JintJsEngine("data/javascript") };

        [Theory]
        [InlineData("{'scalar':'hello','tags':[1,2.123],'page':{'value':3}}", "{'scalar':'hello','tags':[1,2.123],'page':{'value':3}}")]
        [InlineData("{'a':true}", "['a','b']")]
        public void RunJavascript(string input, string output)
        {
            foreach (var engine in _engines)
            {
                Assert.Equal(output.Replace('\'', '"'), engine.Run("index.js", "main", input.Replace('\'', '"')));
            }
        }

        [Theory]
        [InlineData("{'error': true}")]
        public void RunJavascriptError(string input)
        {
            foreach (var engine in _engines)
            {
                Assert.Throws<JavaScriptEngineException>(() => engine.Run("index.js", "main", input.Replace('\'', '"')));
            }
        }
    }
}
