// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.OverwriteDocuments
{
    public sealed class L1InlineCodeHeadingRule :InlineCodeHeadingRule
    {
        public override string Name => nameof(L1InlineCodeHeadingRule);

        protected override bool NeedCheckLevel => true;

        protected override int Level => 1;
    }
}
