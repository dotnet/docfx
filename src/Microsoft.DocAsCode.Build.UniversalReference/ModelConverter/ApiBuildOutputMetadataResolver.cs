// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.DataContracts.UniversalReference;

    using AutoMapper;

    public class ApiBuildOutputMetadataResolver : IValueResolver<ItemViewModel, ApiBuildOutput, Dictionary<string, object>>
    {
        private readonly IReadOnlyDictionary<string, object> _metadata;

        public ApiBuildOutputMetadataResolver(IReadOnlyDictionary<string, object> metadata)
        {
            _metadata = metadata;
        }

        public Dictionary<string, object> Resolve(ItemViewModel source, ApiBuildOutput destination, Dictionary<string, object> destMember, ResolutionContext context)
        {
            return _metadata?.Concat(source.Metadata.Where(p => !_metadata.Keys.Contains(p.Key))).ToDictionary(p => p.Key, p => p.Value) ?? source.Metadata;
        }
    }
}
