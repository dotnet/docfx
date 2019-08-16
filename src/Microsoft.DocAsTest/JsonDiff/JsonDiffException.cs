// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DocAsTest
{
    public class JsonDiffException : Exception
    {
        public string Diff { get; }

        public JsonDiffException(string diff)
            : base($"\n\n{diff}") => Diff = diff;
    }
}
