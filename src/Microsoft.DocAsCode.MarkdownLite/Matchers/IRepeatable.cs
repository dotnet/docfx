// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    internal interface IRepeatable
    {
        Matcher Repeat(int minOccur, int maxOccur);
    }
}
