// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MarkdigEngine.Extensions
{

    public class InclusionContext
    {
        public string Title { get; set; }

        public string IncludedFilePath { get; set; }

        public string GetRaw()
        {
            return $"[!include[{Title}]({IncludedFilePath})]";
        }
    }
}
