// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;

    public class AggregateBlockPathQueryOptionCreator : IDfmFencesBlockPathQueryOptionCreator
    {
        private readonly IDfmFencesBlockPathQueryOptionCreator[] _pathQueryOptionCreaters;

        public AggregateBlockPathQueryOptionCreator(IDfmFencesBlockPathQueryOptionCreator[] pathQueryOptionCreaters = null)
        {
            _pathQueryOptionCreaters = pathQueryOptionCreaters ?? GetDefaultOptionCreaters();
        }
        public IDfmFencesBlockPathQueryOption ParseQueryOrFragment(DfmFencesBlockPathQueryOptionParameters parameters, bool noCache)
        {
            foreach (var creater in _pathQueryOptionCreaters)
            {
                var option = creater.ParseQueryOrFragment(parameters, noCache);
                if (option != null)
                {
                    return option;
                }
            }

            throw new NotSupportedException($"Unable to parse DfmFencesBlockPathQueryOptionParameters");
        }

        public static IDfmFencesBlockPathQueryOptionCreator[] GetDefaultOptionCreaters(CodeLanguageExtractorsBuilder builder = null)
        {
            return new IDfmFencesBlockPathQueryOptionCreator[]
            {
                new FullFileBlockPathQueryOptionCreator(),
                new TagNameBlockPathQueryOptionCreator(builder),
                new MultipleLineRangeBlockPathQueryOptionCreator(),
            };
        }
    }
}
