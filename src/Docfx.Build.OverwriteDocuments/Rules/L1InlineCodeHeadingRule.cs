// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.OverwriteDocuments;

public sealed class L1InlineCodeHeadingRule : InlineCodeHeadingRule
{
    public override string TokenName => "L1InlineCodeHeading";

    protected override bool NeedCheckLevel => true;

    protected override int Level => 1;
}
