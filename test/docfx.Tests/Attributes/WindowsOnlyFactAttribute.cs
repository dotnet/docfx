// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Xunit;

namespace docfx.Tests.Attributes
{
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
}
