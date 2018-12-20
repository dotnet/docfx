# Developer Guide

## Build and Test

Prerequisites:

- [git](https://git-scm.com/)
- [.NET Core SDK 2.2](https://dotnet.microsoft.com/download/dotnet-core/2.2) or above

Build and test this project by running `build.ps1` on Windows, or by running `build.sh` on Mac OS and Linux.

You can use [Visual Studio](https://www.visualstudio.com/vs/) or [Visual Studio Code](https://code.visualstudio.com/) with [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp) to develop the project.

## Release Process

We continously deploy `v3` branch to [Production MyGet Feed](https://www.myget.org/F/docfx-v3/api/v2). It is then deployed to [docs](https://docs.microsoft.com) on a regular cadence. For this to work, `v3` branch **MUST** always be in [Ready to Ship](#definition-of-ready-to-ship) state.

Large feature work happens in feature branches. Feature branch name starts with `feature/`.

Pull request validation, continous deployment to [Sandbox MyGet Feed](https://www.myget.org/F/docfx-v3-sandbox/api/v2) is enabled automatically on `v3` branch and all feature branches.

Package version produced from `v3` branch is higher than feature branches:
- `v3`: `3.0.0-beta-{commitDepth}-{commitHash}`
- `feature/{feature}`: `3.0.0-alpha-{feature}-{commitDepth}-{commitHash}`

We currently do not deploy to NuGet until features blocking community adoption are implemented.

In general we perfer **Squash and merge** against `v3` or feature branches. When merging from feature branches to `v3` with a lot of changes, we prefer **Rebase and merge**.

### Definition of Ready to Ship

- All test cases pass
- No performance regression
- No open issues that affects end users
- No unintended breaking changes *
    - Input output data contract
    - Config
    - Errors and Warnings

    **At this stage, changes to ideal output, config, error message and line number are not considered breaking*

## Coding Guideline

### C#
We follow [C# Coding Style](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md) recommended by dotnet team. Stylecop and FxCop have been enabled for this project to enforce some of the rules.

Besides that, we have some recommended but not mandatory (that is, not enabled by StyleCop/FxCop) rules as illustrated below.

#### Conventions
* **DO** use `sealed` for private classes if they are not to be inherited.
* **DO** use `static` methods if it is not instance relevant.
* **DO** make sure `static` methods are thread safe.

#### *Sealed* classes
Seal the class when it is not designed for extensibility. *When designing, it may be a good idea to lean towards sealing public types that don't explicitly need to be extended since unsealing a class in a future version is a non-breaking change while the reverse is not true.* In general, private classes can be `private sealed class` if they are not to be inherited.

#### Regex
Be careful about `RegexOptions.Compiled`. Generally if the regex expression is `static` `readonly` one to be shared and regularly reused, make it `RegexOptions.Compiled`. But when the regex expression is an instance object and the pattern always changes, because of the overhead of object instantiation and regular expression compilation, creating and rapidly destroying numerous Regex objects is a very expensive process. Details refer to https://docs.microsoft.com/en-us/dotnet/standard/base-types/compilation-and-reuse-in-regular-expressions#the-regular-expressions-cache

#### Unit tests and functional tests
##### Assembly naming
The unit tests for the `Microsoft.Foo` assembly live in the `Microsoft.Foo.Tests` assembly.

The functional tests for the `Microsoft.Foo` assembly live in the `Microsoft.Foo.FunctionalTests` assembly.

In general there should be exactly one unit tests assembly for each product runtime assembly. In general there should be one functional tests assembly per repo. Exceptions can be made for both.

##### Unit test class naming
Test class names end with `Test` suffix and live in the same namespace as the class being tested. For example, the unit tests for the `Microsoft.Foo.Boo` class would be in a `Microsoft.Foo.BooTest` class in the unit tests assembly `Microsoft.Foo.Tests`.

##### Unit test method naming
Unit test method names must be descriptive about *what developers are testing, under what conditions, and what the expectations are*. The following test name is correct:

```cs
PublicApiArgumentsShouldHaveNotNullAnnotation
```

The following test names are incorrect:

```cs
Test1
Constructor
FormatString
GetData
```

##### Unit test structure
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

##### Testing exception messages

Testing the specific exception message in a unit test is important. This ensures that the desired exception is being tested rather than a different exception of the same type. In order to verify the exact exception, it is important to verify the message.

```cs
// Act
var ex = Assert.Throws<InvalidOperationException>(() => fruitBasket.GetBananaById(-1));

// Assert
Assert.Equal("Cannot load banana with negative identifier.", ex.Message);
```

##### Use xUnit.net's plethora of built-in assertions
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

##### Parallel tests
By default all unit test assemblies should run in parallel mode, which is the default. Unit tests shouldn't depend on any shared state, and so should generally be runnable in parallel. If tests fail in parallel, the first thing to do is to figure out why; do not just disable parallel testing!

For functional tests, you can disable parallel tests.

