// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class MonikerComparer : IComparer<string>, IEqualityComparer<string>
    {
        private readonly Dictionary<string, int> _monikerOrder;

        public MonikerComparer(MonikerDefinitionModel monikerDefinition)
        {
            _monikerOrder = GetMoninkerOrder(monikerDefinition.Monikers);
        }

        public int Compare(string x, string y)
        {
            int result;
            if (x is null || !TryGetMonikerOrderFromDefinition(x, out var orderX))
            {
                result = -1;
            }
            else if (y is null || !TryGetMonikerOrderFromDefinition(y, out var orderY))
            {
                result = 1;
            }
            else
            {
                result = orderX.CompareTo(orderY);
            }
            return result;
        }

        public bool Equals(string x, string y)
        {
            return Compare(x, y) == 0;
        }

        public int GetHashCode(string obj)
        {
            return obj.GetHashCode();
        }

        private bool TryGetMonikerOrderFromDefinition(string moniker, out int order)
            => _monikerOrder.TryGetValue(moniker, out order);

        private Dictionary<string, int> GetMoninkerOrder(List<Moniker> monikers)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < monikers.Count; i++)
            {
                result[monikers[i].MonikerName] = i;
            }
            return result;
        }
    }
}
