// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace docfx.Tests.Attributes;

/// <summary>
/// XUnit Fact attribute that skips the test if the OS is not Windows.
/// </summary>
/// <remarks>
/// Taken from https://github.com/dotnet/sdk/blob/a30e465a2e2ea4e2550f319a2dc088daaafe5649/src/Tests/Microsoft.NET.TestFramework/Attributes/WindowsOnlyFactAttribute.cs
/// </remarks>
public class WindowsOnlyFactAttribute : FactAttribute
{
    public WindowsOnlyFactAttribute()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Skip = "This test requires Windows to run";
        }
    }
}
