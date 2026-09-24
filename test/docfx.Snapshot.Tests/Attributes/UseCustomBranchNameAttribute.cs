// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit.v3;

namespace Docfx.Tests;

internal class UseCustomBranchNameAttribute(string branchName) : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (test.TestCase.TestCollection.TestCollectionDisplayName != "docfx STA")
            throw new InvalidOperationException(@"UseCustomBranchNameAttribute change global context. Use `[Collection(""docfx STA"")]` to avoid parallel test executions.");

        Environment.SetEnvironmentVariable("DOCFX_SOURCE_BRANCH_NAME", branchName);
    }

    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        Environment.SetEnvironmentVariable("DOCFX_SOURCE_BRANCH_NAME", null);
    }
}
