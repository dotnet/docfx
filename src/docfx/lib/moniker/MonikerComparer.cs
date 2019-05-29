// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal class MonikerComparer : IComparer<string>
    {
        private readonly Dictionary<string, (string productName, int orderInProduct)> _monikerOrder;

        public MonikerComparer(Dictionary<string, (string, int)> monikerOrder)
        {
            _monikerOrder = monikerOrder;
        }

        public object Assert { get; private set; }

        public int Compare(string x, string y)
        {
            if (x is null || y is null || !_monikerOrder.ContainsKey(x) || !_monikerOrder.ContainsKey(y))
            {
                Debug.Fail("Should not be here");
            }
            var orderX = _monikerOrder[x];
            var orderY = _monikerOrder[y];
            if (orderX.productName != orderY.productName)
            {
                return string.Compare(orderX.productName, orderY.productName);
            }
            return _monikerOrder[x].orderInProduct.CompareTo(_monikerOrder[y].orderInProduct);
        }
    }
}
