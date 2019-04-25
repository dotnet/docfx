// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class JsonSchemaTest
    {
        [Theory]
        [InlineData("{'type': 'boolean'}", "true", "")]
        [InlineData("{'type': 'array'}", "[]", "")]
        [InlineData("{'type': 'object'}", "{}", "")]
        [InlineData("{'type': 'string'}", "'text'", "")]
        [InlineData("{'type': 'integer'}", "123", "")]
        [InlineData("{'type': 'number'}", "123.456", "")]
        [InlineData("{'type': 'boolean'}", "'string'",
            "['error','violate-schema','Expected type Boolean, please input Boolean or type compatible with Boolean.','file',1,8]")]
        [InlineData("{'type': 'object'}", "1",
            "['error','violate-schema','Expected type Object, please input Object or type compatible with Object.','file',1,1]")]
        [InlineData("{'type': 'string'}", "1",
            "['error','violate-schema','Expected type String, please input String or type compatible with String.','file',1,1]")]
        public void TestJsonSchemaValidation(string schema, string json, string expected)
        {
            var errors = JsonSchemaValidation.Validate(
                JsonUtility.Deserialize<JsonSchema>(schema.Replace('\'', '"')),
                JsonUtility.Parse(json.Replace('\'', '"'), "file").Item2);

            var actual = string.Join('\n', errors.Select(err => err.ToString()).OrderBy(err => err).ToArray()).Replace('"', '\'');
            Assert.Equal(expected, actual);
        }
    }
}
