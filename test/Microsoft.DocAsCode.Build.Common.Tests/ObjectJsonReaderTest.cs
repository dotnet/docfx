// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Common;

    using Xunit;
    using Newtonsoft.Json;

    [Trait("Owner", "lianwei")]
    public class ObjectJsonReaderTest
    {
        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        [InlineData(2.0)]
        [InlineData("hello world")]
        public void TestObjectJsonReader(object model)
        {
            AssertEqual(model);
        }

        [Fact]
        public void TestComplexJsonReader()
        {
            AssertEqual(new DateTime());
            AssertEqual(new List<object>());
            AssertEqual(new Dictionary<string, object>());
            AssertEqual(new Dictionary<object, object>());

            AssertEqual(new List<object> { "1", "2", new List<object> { 1.0, 2 } });

            var model = new Dictionary<string, object>
            {
                ["a"] = "A",
                ["b"] = new Dictionary<string, object>
                {
                    ["a"] = "A"
                }
            };
            var model2 = new List<object>
            {
                model,
                model
            };
            var model3 = new Dictionary<string, object>
            {
                ["model10"] = model,
                ["model2"] = model2
            };
            var model4 = new List<object>
            {
                model,
                model2,
                model3,
            };

            AssertEqual(model);
            AssertEqual(model2);
            AssertEqual(model3);
            AssertEqual(model4);
        }

        [Fact]
        public void TestComplexJsonReaderWithStrongTypeObject()
        {
            var model = new Dictionary<string, object>
            {
                ["a"] = new StrongTypeClass { Value = "A" },
                ["b"] = new Dictionary<string, object>
                {
                    ["a"] = "A"
                },
                ["c"] = null,
            };

            var expectedModel = new Dictionary<string, object>
            {
                ["a"] = null,
                ["b"] = new Dictionary<string, object>
                {
                    ["a"] = "A"
                },
                ["c"] = null,
            };

            AssertEqual(model, expectedModel);
        }

        private class StrongTypeClass
        {
            public string Value { get; set; }
        }

        private void AssertEqual(object model, object expectedModel = null)
        {
            expectedModel = expectedModel ?? model;
            Assert.Equal(GetExpectedLines(expectedModel), GetActualLines(model));

        }

        static IEnumerable<string> GetExpectedLines(object model)
        {
            JsonReader reader = new JsonTextReader(new StringReader(JsonConvert.SerializeObject(model)));
            while (reader.Read())
            {
                yield return ($"{reader.TokenType}:{reader.Value}");
            }
        }

        static IEnumerable<string> GetActualLines(object model)
        {
            var reader = new IgnoreStrongTypeObjectJsonReader(model);

            while (reader.Read())
            {
                yield return ($"{reader.TokenType}:{reader.Value}");
            }
        }
    }
}
