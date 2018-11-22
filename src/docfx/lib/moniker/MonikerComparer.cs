// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class MonikerComparer : IComparer<string>
    {
        private readonly MonikerRangeParser _parser;
        private readonly bool _ascending;

        public MonikerComparer(MonikerRangeParser parser, bool ascending = true)
        {
            _parser = parser;
            _ascending = ascending;
        }

        public int Compare(string x, string y)
        {
            int result;
            if (x is null || !_parser.TryGetMonikerOrderFromDefinition(x, out var orderX))
            {
                result = -1;
            }
            else if (y is null || !_parser.TryGetMonikerOrderFromDefinition(y, out var orderY))
            {
                result = 1;
            }
            else
            {
                result = orderX.CompareTo(orderY);
            }
            return _ascending ? result : -result;
        }
    }
}
