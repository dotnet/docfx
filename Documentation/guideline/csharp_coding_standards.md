---
uid: csharp_coding_standards
---

C# Coding Standards
====================

Introduction
----------------
The C# Coding Standards will be used in conjunction with customized versions of *StyleCop* and *FxCop* [**TODO**] during both development and build process. This will help ensure that all developers on the team are in a consistent manner.

>"Any fool can write code that a computer can understand. Good programmers write code that humans understand".
>
> Martin Fowler. *Refactoring: Improving the design of existing code.*

### Purpose

This section defines a set of C# coding standards to be used by the DocFX build team to guarantee maximum legibility, reliability, re-usability and homogeneity of our code. Each section is marked *Mandatory* or *Recommended*. *Mandatory* sections will be enforced during code reviews as well as tools like *StyleCop* and *FxCop*, and code will not be considered complete until it is compliant.

### Scope

This section contains general C# coding standards that can be applied to any type of application developed in C#, based upon *[Framework Design Guidelines](http://msdn.microsoft.com/en-us/library/ms229042.aspx)*.

This section is not intended to be a tutorial on C#. Instead, it includes a set of limitations and recommendations focused on clarifying the development.

Tools
----------------

* [ReSharper](http://www.jetbrains.com/resharper/) is a useful third-party code cleanup and style tool.
* [StyleCop](http://stylecop.codeplex.com/) analyzes C# source code to enforce a set of style and consistency rules and has been integrated into many third-party development tools such as ReSharper.
* [FxCop](http://codebox/SDLFxCop) is an application that analyzes managed code assemblies (code that targets the .NET Framework common language runtime) and reports information about the assemblies, such as possible design, localization, performance, and security improvements.

Highlights of Coding Standards
------------------------------

This section is not intended to give a summary of all the coding standards enabled by our customized StyleCop, but to give a highlight of some rules one will possibly meet in daily coding life. It also provides some coding standards that are recommended but not mandatory (that is, not enabled by StyleCop).

### File Layout (Recommended)
Only one public class is allowed per file.

The file name derives from the class name.

    Class   : Observer
    Filename: Observer.cs

### Class Definition Order (Mandatory)

The class definition contains class members in the following order, from *less* restricted scope (public) to *more* restrictive (private):

* ~~~ Nested types, e.g. classes, enum, struct, etc.~~~ Non-private nested types are not allowed.
* Field members (for example, member variables, const, etc.)
* Member functions
  * Constructors
  * Finalizer (Do not use unless absolutely necessary)
  * Methods (Properties, Events, Operations, Overridables and Static)
  * Private nested types


### Naming (Mandatory)
* **DO** use plural form for namespaces
* **DO** use PascalCasing for all public member, type, and namespace names consisting of multiple words.

    PropertyDescriptor
    HtmlTag
    IOStream

  > **Note**
  >
  > A special case is made for two-letter acronyms in which both letters are capitalized, e.g. *IOStream*

* **DO** use camelCasing for parameter names.

    propertyDescriptor
    htmlTag
    ioStream

* **DO** start with underscore for private fields:

    private readonly Guid _userId = Guid.NewGuid();

* **DO** start static readonly field and constant names with capitalized case

    private static readonly IEntityAccessor EntityAccessor = null;
        private const string MetadataName = "MetadataName";

* **DO NOT** capitalize each word in so-called [closed-form compound words](http://msdn.microsoft.com/en-us/library/ms229043.aspx).

* **DO** use `Async` suffix in the asynchronous method names to notice people how to use it properly

      public async Task<string> LoadContentAsync() { ... }

### Formatting (Mandatory)
* **DO** use spaces over tabs, and always show all spaces/tabs in IDE

> **Tips**
>
  > Visual Studio > Tools > Options... > Text Editor > C# > Tabs > Insert spaces (Tab size: 4)
>
  > Visual Studio > Edit > Advanced > View White Space (Ctrl+R, Ctrl+W)

* **DO** add *using* inside *namespace* declaration

    namespace Microsoft.Content.Build.BuildWorker.UnitTest
    {
      using System;
    }

* **DO** add a space when:
  1. `for (var i = 0; i < 1; i++)`
  2. `if (a == b)`

### Performace Consideration
* **DO** use `sealed` for private classes if they are not to be inherited.
* **DO** add `readonly` to fields if they do not tend to be changed.
* **DO** use `static` methods if it is not instance relevant.
* **DO** use `RegexOptions.Compiled` for `readonly` `Regex`.

### Cross-platform coding
Our code can and should support multiple operating systems in addition to Windows. Code should be sensitvie to the differences between Operating Systems. Here are some specifics to consider:

* **DO** use `Enviroment.NewLine` instead of hard-coding the line break, as Windows uses `\r\n` and OSX/Linux uses `\n`.

> **Note**
>
  > Be aware that these line-endings may cause problems in code when using `@""` text blocks with line breaks, e.g.:
  >
  > ```cs
  > var x = @"line1
  > line2";
  > ```

* **DO** use `Path.Combine()` or `Path.DirectorySeparatorChar` to separate directories. If this is not possible (such as in scripting), use a forward slash `/`. Windows is more forgiving than Linux in this regard.

### Unit tests and functional tests
#### Assembly naming
The unit tests for the `Microsoft.Foo` assembly live in the `Microsoft.Foo.Tests` assembly.

The functional tests for the `Microsoft.Foo` assembly live in the `Microsoft.Foo.FunctionalTests` assembly.

In general there should be exactly one unit tests assembly for each product runtime assembly. In general there should be one functional tests assembly per repo. Exceptions can be made for both.

#### Unit test class naming
Test class names end with `Test` suffix and live in the same namespace as the class being tested. For example, the unit tests for the `Microsoft.Foo.Boo` class would be in a `Microsoft.Foo.BooTest` class in the unit tests assembly `Microsoft.Foo.Tests`.

#### Unit test method naming
Unit test method names must be descriptive about *what developers are testing, under what conditions, and what the expectations are*. Pascal casing and underscores can be used to improve readability. The following test names are correct:

```cs
PublicApiArgumentsShouldHaveNotNullAnnotation
Public_api_arguments_should_have_not_null_annotation
```

The following test names are incorrect:

```cs
Test1
Constructor
FormatString
GetData
```

#### Unit test structure
The contents of every unit test should be split into three distinct stages (arrange, act and assert), optionally separated by these comments:

```cs
// Arrange
// Act
// Assert
```

The crucial thing here is the `Act` stage is exactly one statement. That one statement calls only the one method that you are trying to test. Keeping that one statement as simple as possible is also very important. For example, this is not ideal:

```cs
int result = myObj.CallSomeMethod(GetComplexParam1(), GetComplexParam2(), GetComplexParam3());
```

This style is not recommended because too much can go wrong in this one statement. All the `GetComplexParamN()` calls can throw exceptions for a variety of reasons unrelated to the test itself. It is thus unclear to someone running into a problem why the failure occurred.

The ideal pattern is to move the complex parameter building into the `Arrange` section:

```cs
// Arrange
P1 p1 = GetComplexParam1();
P2 p2 = GetComplexParam2();
P3 p3 = GetComplexParam3();

// Act
int result = myObj.CallSomeMethod(p1, p2, p3);

// Assert
Assert.AreEqual(1234, result);
```

Now the only reason the line with `CallSomeMethod()` can fail is if the method itself throws an error.

#### Testing exception messages

Testing the specific exception message in a unit test is important. This ensures that the desired exception is being tested rather than a different exception of the same type. In order to verify the exact exception, it is important to verify the message.

```cs
// Act
var ex = Assert.Throws<InvalidOperationException>(() => fruitBasket.GetBananaById(-1));

// Assert
Assert.Equal("Cannot load banana with negative identifier.", ex.Message);
```

#### Use xUnit.net's plethora of built-in assertions
xUnit.net includes many kinds of assertions – please use the most appropriate one for your test. This makes the tests much more readable and also allows the test runner to report the best possible errors (whether it's local or the CI machine). For example, these are bad:

```cs
Assert.Equal(true, someBool);

Assert.True("abc123" == someString);

Assert.True(list1.Length == list2.Length);

for (int i = 0; i < list1.Length; i++) {
    Assert.True(
        String.Equals(
            list1[i],
            list2[i],
            StringComparison.OrdinalIgnoreCase));
}
```

These are good:

```cs
Assert.True(someBool);

Assert.Equal("abc123", someString);

// built-in collection assertions!
Assert.Equal(list1, list2, StringComparer.OrdinalIgnoreCase);
```

#### Parallel tests
By default all unit test assemblies should run in parallel mode, which is the default. Unit tests shouldn't depend on any shared state, and so should generally be runnable in parallel. If tests fail in parallel, the first thing to do is to figure out why; do not just disable parallel testing!

For functional tests, you can disable parallel tests.
