// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class MonikerComparer : IComparer<string>
    {
        private readonly bool _ascending;
        private readonly Dictionary<string, int> _monikerOrder;

        public MonikerComparer(MonikerDefinitionModel monikerDefinition, bool ascending = true)
        {
            _monikerOrder = GetMoninkerOrder(monikerDefinition.Monikers);
            _ascending = ascending;
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
            return _ascending ? result : -result;
        }

        private bool TryGetMonikerOrderFromDefinition(string moniker, out int order)
            => _monikerOrder.TryGetValue(moniker, out order);

        private Dictionary<string, int> GetMoninkerOrder(List<Moniker> monikers)
        {
            var result = new Dictionary<string, int>();
            for (int i = 0; i < monikers.Count; i++)
            {
                result[monikers[i].Name] = i;
            }
            return result;
        }
    }
}
