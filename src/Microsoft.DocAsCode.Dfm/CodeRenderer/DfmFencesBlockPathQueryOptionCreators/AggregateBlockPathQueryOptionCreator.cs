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
            _pathQueryOptionCreaters = pathQueryOptionCreaters ?? new IDfmFencesBlockPathQueryOptionCreator[]
            {
                new FullFileBlockPathQueryOptionCreator(),
                new TagNameBlockPathQueryOptionCreator(),
                new MultipleLineRangeBlockPathQueryOptionCreator(),
            };
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
    }
}
