// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    public class TagNameBlockPathQueryOptionCreator : IDfmFencesBlockPathQueryOptionCreator
    {
        private readonly CodeLanguageExtractorsBuilder _builder;

        public TagNameBlockPathQueryOptionCreator(CodeLanguageExtractorsBuilder builder = null)
        {
            _builder = builder;
        }

        public IDfmFencesBlockPathQueryOption ParseQueryOrFragment(
            DfmFencesBlockPathQueryOptionParameters parameters,
            bool noCache = false)
        {
            if (parameters == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(parameters.TagName))
            {
                return new TagNameBlockPathQueryOption(_builder, noCache)
                {
                    HighlightLines = parameters.HighlightLines,
                    DedentLength = parameters.DedentLength,
                    TagName = parameters.TagName,
                };
            }

            return null;
        }
    }
}
