// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Tests
{
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Tests.Common;

    using Xunit;

    [Trait("Owner", "lianwei")]
    public class JsonPointerTest : TestBase
    {
        [Fact]
        public void TestJsonPointerSpec()
        {
            var root = ConvertToObjectHelper.ConvertToDynamic(ConvertToObjectHelper.ConvertJObjectToObject(JsonUtility.FromJsonString<object>(@"
{
      ""foo"": [""bar"", ""baz""],
      """": 0,
      ""a/b"": 1,
      ""c%d"": 2,
      ""e^f"": 3,
      ""g|h"": 4,
      ""i\\j"": 5,
      ""k\""l"": 6,
      "" "": 7,
      ""m~n"": 8
   }
")));

            Assert.Equal(root, new JsonPointer("").GetValue(root));
            Assert.Equal(((dynamic)root).foo, new JsonPointer("/foo").GetValue(root));
            Assert.Equal("bar", new JsonPointer("/foo/0").GetValue(root));
            Assert.Equal(0L, new JsonPointer("/").GetValue(root));
            Assert.Equal(1L, new JsonPointer("/a~1b").GetValue(root));
            Assert.Equal(2L, new JsonPointer("/c%d").GetValue(root));
            Assert.Equal(3L, new JsonPointer("/e^f").GetValue(root));
            Assert.Equal(4L, new JsonPointer("/g|h").GetValue(root));
            Assert.Equal(5L, new JsonPointer("/i\\j").GetValue(root));
            Assert.Equal(6L, new JsonPointer("/k\"l").GetValue(root));
            Assert.Equal(7L, new JsonPointer("/ ").GetValue(root));
            Assert.Equal(8L, new JsonPointer("/m~0n").GetValue(root));
        }

        [Fact]
        public void TestJsonPointerWithComplexObject()
        {
            var root = ConvertToObjectHelper.ConvertToDynamic(ConvertToObjectHelper.ConvertJObjectToObject(JsonUtility.FromJsonString<object>(@"
{
      ""dict"": {
        ""key1"": ""value1"",
        ""key2"": [""arr1"", ""arr2""],
        ""key3"": {
            ""key1"": ""value1"",
            ""key2"": [""arr1"", ""arr2""],
            ""key3"": {
                ""key1"": ""value1"",
                ""key2"": [""arr1"", ""arr2""],
                ""key3"": {
                   ""key1"": ""value1""
                }
            }
        }
    },
      ""array"": [""bar"", ""baz""]
   }
")));

            Assert.Equal(root, new JsonPointer("").GetValue(root));
            Assert.Equal("value1", new JsonPointer("/dict/key1").GetValue(root));
            Assert.Equal("arr2", new JsonPointer("/dict/key2/1").GetValue(root));
            Assert.Equal("value1", new JsonPointer("/dict/key3/key3/key3/key1").GetValue(root));
            Assert.Null(new JsonPointer("/dict/key4").GetValue(root));
            Assert.Null(new JsonPointer("/dict/key4/key1").GetValue(root));
            Assert.Null(new JsonPointer("/dict/key2/2").GetValue(root));

            var jp = new JsonPointer("/dict/key1");
            jp.SetValue(ref root, 1);
            Assert.Equal(1, jp.GetValue(root));

            jp = new JsonPointer("/dict/key3/key2/1");
            jp.SetValue(ref root, 2);
            Assert.Equal(2, jp.GetValue(root));

            jp = new JsonPointer("");
            jp.SetValue(ref root, 3);
            Assert.Equal(3, root);
            Assert.Equal(3, jp.GetValue(root));

            Assert.Throws<InvalidJsonPointerException>(() => new JsonPointer("/dict/key2/2").SetValue(ref root, 1));
        }
    }
}
