// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    public static class FilterVisitorExtensions
    {
        public static IFilterVisitor WithConfig(this IFilterVisitor fv, string configFile)
        {
            return new ConfigFilterVisitor(fv, configFile);
        }

        public static IFilterVisitor WithConfig(this IFilterVisitor fv, ConfigFilterRule rule)
        {
            return new ConfigFilterVisitor(fv, rule);
        }

        public static IFilterVisitor WithCache(this IFilterVisitor fv)
        {
            return new CachedFilterVisitor(fv);
        }
    }
}
