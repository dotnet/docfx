// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    public class MultipleLineRangeBlockPathQueryOptionCreator : IDfmFencesBlockPathQueryOptionCreator
    {
        public IDfmFencesBlockPathQueryOption ParseQueryOrFragment(
            DfmFencesBlockPathQueryOptionParameters parameters,
            bool noCache = false)
        {
            if (parameters == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(parameters.TagName) && parameters.LinePairs.Count > 0)
            {
                return new MultipleLineRangeBlockPathQueryOption
                {
                    HighlightLines = parameters.HighlightLines,
                    DedentLength = parameters.DedentLength,
                    LinePairs = parameters.LinePairs,
                };
            }

            return null;
        }
    }
}
