// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.Common.Tests;

[TestClass]
public class ReflectionHelperTest
{
    [TestMethod]
    public void TestGetPropertyValueForClass()
    {
        var obj = new MyClass
        {
            MyProperty1 = "Hello",
            MyProperty2 = 100,
            MyProperty3 = null,
        };
        Assert.AreEqual("Hello", ReflectionHelper.GetPropertyValue(obj, typeof(MyClass).GetProperty(nameof(MyClass.MyProperty1))));
        Assert.AreEqual(100, ReflectionHelper.GetPropertyValue(obj, typeof(MyClass).GetProperty(nameof(MyClass.MyProperty2))));
        Assert.IsNull(ReflectionHelper.GetPropertyValue(obj, typeof(MyClass).GetProperty(nameof(MyClass.MyProperty3))));
    }

    [TestMethod]
    public void TestGetPropertyValueForStructure()
    {
        var obj = new MyStruct
        {
            MyProperty1 = "Hello",
            MyProperty2 = 100,
            MyProperty3 = null,
        };
        Assert.AreEqual("Hello", ReflectionHelper.GetPropertyValue(obj, typeof(MyStruct).GetProperty(nameof(MyStruct.MyProperty1))));
        Assert.AreEqual(100, ReflectionHelper.GetPropertyValue(obj, typeof(MyStruct).GetProperty(nameof(MyStruct.MyProperty2))));
        Assert.IsNull(ReflectionHelper.GetPropertyValue(obj, typeof(MyStruct).GetProperty(nameof(MyStruct.MyProperty3))));
    }

    [TestMethod]
    public void TestGetPropertyValueForClassByInterface()
    {
        var obj = new MyClass
        {
            MyProperty1 = "Hello",
            MyProperty2 = 100,
            MyProperty3 = null,
        };
        Assert.AreEqual("Hello", ReflectionHelper.GetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyInterface.MyProperty1))));
        Assert.AreEqual(100, ReflectionHelper.GetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyInterface.MyProperty2))));
        Assert.IsNull(ReflectionHelper.GetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyInterface.MyProperty3))));
    }

    [TestMethod]
    public void TestGetPropertyValueForStructureByInterface()
    {
        var obj = new MyStruct
        {
            MyProperty1 = "Hello",
            MyProperty2 = 100,
            MyProperty3 = null,
        };
        Assert.AreEqual("Hello", ReflectionHelper.GetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyInterface.MyProperty1))));
        Assert.AreEqual(100, ReflectionHelper.GetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyInterface.MyProperty2))));
        Assert.IsNull(ReflectionHelper.GetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyInterface.MyProperty3))));
    }

    [TestMethod]
    public void TestSetPropertyValueForClass()
    {
        var obj = new MyClass();
        ReflectionHelper.SetPropertyValue(obj, typeof(MyClass).GetProperty(nameof(MyClass.MyProperty1)), "Hello");
        Assert.AreEqual("Hello", obj.MyProperty1);
        ReflectionHelper.SetPropertyValue(obj, typeof(MyClass).GetProperty(nameof(MyClass.MyProperty1)), null);
        Assert.IsNull(obj.MyProperty1);
        ReflectionHelper.SetPropertyValue(obj, typeof(MyClass).GetProperty(nameof(MyClass.MyProperty2)), 100);
        Assert.AreEqual(100, obj.MyProperty2);
        ReflectionHelper.SetPropertyValue(obj, typeof(MyClass).GetProperty(nameof(MyClass.MyProperty3)), true);
        Assert.IsTrue(obj.MyProperty3);
        ReflectionHelper.SetPropertyValue(obj, typeof(MyClass).GetProperty(nameof(MyClass.MyProperty3)), null);
        Assert.IsNull(obj.MyProperty3);
    }

    [TestMethod]
    public void TestSetPropertyValueForStructure()
    {
        object obj = new MyStruct();
        ReflectionHelper.SetPropertyValue(obj, typeof(MyStruct).GetProperty(nameof(MyStruct.MyProperty1)), "Hello");
        Assert.AreEqual("Hello", ((MyStruct)obj).MyProperty1);
        ReflectionHelper.SetPropertyValue(obj, typeof(MyStruct).GetProperty(nameof(MyStruct.MyProperty1)), null);
        Assert.IsNull(((MyStruct)obj).MyProperty1);
        ReflectionHelper.SetPropertyValue(obj, typeof(MyStruct).GetProperty(nameof(MyStruct.MyProperty2)), 100);
        Assert.AreEqual(100, ((MyStruct)obj).MyProperty2);
        ReflectionHelper.SetPropertyValue(obj, typeof(MyStruct).GetProperty(nameof(MyStruct.MyProperty3)), true);
        Assert.IsTrue(((MyStruct)obj).MyProperty3);
        ReflectionHelper.SetPropertyValue(obj, typeof(MyStruct).GetProperty(nameof(MyStruct.MyProperty3)), null);
        Assert.IsNull(((MyStruct)obj).MyProperty3);
    }

    [TestMethod]
    public void TestSetPropertyValueForClassByInterface()
    {
        var obj = new MyClass();
        ReflectionHelper.SetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyInterface.MyProperty1)), "Hello");
        Assert.AreEqual("Hello", obj.MyProperty1);
        ReflectionHelper.SetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyInterface.MyProperty1)), null);
        Assert.IsNull(obj.MyProperty1);
        ReflectionHelper.SetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyInterface.MyProperty2)), 100);
        Assert.AreEqual(100, obj.MyProperty2);
        ReflectionHelper.SetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyInterface.MyProperty3)), true);
        Assert.IsTrue(obj.MyProperty3);
        ReflectionHelper.SetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyInterface.MyProperty3)), null);
        Assert.IsNull(obj.MyProperty3);
    }

    [TestMethod]
    public void TestSetPropertyValueForStructureByInterface()
    {
        MyInterface obj = new MyStruct();
        ReflectionHelper.SetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyInterface.MyProperty1)), "Hello");
        Assert.AreEqual("Hello", obj.MyProperty1);
        ReflectionHelper.SetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyInterface.MyProperty1)), null);
        Assert.IsNull(obj.MyProperty1);
        ReflectionHelper.SetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyInterface.MyProperty2)), 100);
        Assert.AreEqual(100, obj.MyProperty2);
        ReflectionHelper.SetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyInterface.MyProperty3)), true);
        Assert.IsTrue(obj.MyProperty3);
        ReflectionHelper.SetPropertyValue(obj, typeof(MyInterface).GetProperty(nameof(MyInterface.MyProperty3)), null);
        Assert.IsNull(obj.MyProperty3);
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
