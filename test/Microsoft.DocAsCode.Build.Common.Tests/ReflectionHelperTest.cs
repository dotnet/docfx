// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common.Tests
{
    using Xunit;

    using Microsoft.DocAsCode.Build.Common;

    [Trait("Owner", "vwxyzh")]
    public class ReflectionHelperTest
    {
        [Fact]
        public void TestGetPropertyValue_FromClass()
        {
            var obj = new MyClass
            {
                MyProperty1 = "Hello",
                MyProperty2 = 100,
                MyProperty3 = null,
            };
            Assert.Equal("Hello", ReflectionHelper.GetPropertyValue(obj, typeof(MyClass).GetProperty(nameof(MyClass.MyProperty1))));
            Assert.Equal(100, ReflectionHelper.GetPropertyValue(obj, typeof(MyClass).GetProperty(nameof(MyClass.MyProperty2))));
            Assert.Equal(null, ReflectionHelper.GetPropertyValue(obj, typeof(MyClass).GetProperty(nameof(MyClass.MyProperty3))));
        }

        [Fact]
        public void TestGetPropertyValue_FromStructure()
        {
            var obj = new MyStruct
            {
                MyProperty1 = "Hello",
                MyProperty2 = 100,
                MyProperty3 = null,
            };
            Assert.Equal("Hello", ReflectionHelper.GetPropertyValue(obj, typeof(MyStruct).GetProperty(nameof(MyClass.MyProperty1))));
            Assert.Equal(100, ReflectionHelper.GetPropertyValue(obj, typeof(MyStruct).GetProperty(nameof(MyClass.MyProperty2))));
            Assert.Equal(null, ReflectionHelper.GetPropertyValue(obj, typeof(MyStruct).GetProperty(nameof(MyClass.MyProperty3))));
        }

        [Fact]
        public void TestGetPropertyValue_FromClassByInterface()
        {
            var obj = new MyClass
            {
                MyProperty1 = "Hello",
                MyProperty2 = 100,
                MyProperty3 = null,
            };
            Assert.Equal("Hello", ReflectionHelper.GetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyClass.MyProperty1))));
            Assert.Equal(100, ReflectionHelper.GetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyClass.MyProperty2))));
            Assert.Equal(null, ReflectionHelper.GetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyClass.MyProperty3))));
        }

        [Fact]
        public void TestGetPropertyValue_FromStructureByInterface()
        {
            var obj = new MyStruct
            {
                MyProperty1 = "Hello",
                MyProperty2 = 100,
                MyProperty3 = null,
            };
            Assert.Equal("Hello", ReflectionHelper.GetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyClass.MyProperty1))));
            Assert.Equal(100, ReflectionHelper.GetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyClass.MyProperty2))));
            Assert.Equal(null, ReflectionHelper.GetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyClass.MyProperty3))));
        }

        public class MyClass : MyInterface
        {
            public string MyProperty1 { get; set; }
            public int MyProperty2 { get; set; }
            public bool? MyProperty3 { get; set; }
        }

        public struct MyStruct : MyInterface
        {
            public string MyProperty1 { get; set; }
            public int MyProperty2 { get; set; }
            public bool? MyProperty3 { get; set; }
        }

        public interface MyInterface
        {
            string MyProperty1 { get; set; }
            int MyProperty2 { get; set; }
            bool? MyProperty3 { get; set; }
        }
    }
}
