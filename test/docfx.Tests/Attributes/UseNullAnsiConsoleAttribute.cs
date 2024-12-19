// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Spectre.Console;
using Xunit.v3;

namespace Docfx.Tests;

/// <summary>
/// Custom <see cref="BeforeAfterTestAttribute"/> to temporary suppress <see cref="AnsiConsole.ConsoleAnsiConsole"/> output.
/// This attribute is required to run unit tests that calling `Program.Main` with `--help` arguments.
/// 
/// This issued is confirmed by using `Visual Studio Version 17.13.0 Preview 2.0`
/// It's expected to be resolved in future releases.
/// </summary>
internal class UseNullAnsiConsoleAttribute : BeforeAfterTestAttribute
{
    private static readonly IAnsiConsole NullConsole = AnsiConsole.Create(new AnsiConsoleSettings
    {
        Out = new AnsiConsoleOutput(StringWriter.Null),
    });

    private IAnsiConsole SavedConsole;

    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (test.TestCase.TestCollection.TestCollectionDisplayName != "docfx STA")
            throw new InvalidOperationException(@"UseNullAnsiConsoleAttribute change global context. Use `[Collection(""docfx STA"")]` to avoid parallel test executions.");

        SavedConsole = AnsiConsole.Console;
        AnsiConsole.Console = NullConsole;
    }

    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        AnsiConsole.Console = SavedConsole;
    }
}
