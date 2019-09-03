// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DocAsTest
{
    public class JsonDiffException : Exception
    {
        public string Summary { get; }

        public string Diff { get; }

        public JsonDiffException(string summary, string diff)
            : base($"{summary}\n\n{diff}")
        {
            Summary = summary;
            Diff = diff;
        }
    }
}
