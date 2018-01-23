// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.OverwriteDocuments
{
    public sealed class L1InlineCodeHeadingRule :InlineCodeHeadingRule
    {
        public override string TokenName => "L1InlineCodeHeading";

        protected override bool NeedCheckLevel => true;

        protected override int Level => 1;
    }
}
