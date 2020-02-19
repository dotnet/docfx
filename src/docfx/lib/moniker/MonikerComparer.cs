// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class MonikerComparer : IComparer<string>
    {
        private readonly Dictionary<string, (string productName, int orderInProduct)> _monikerOrder;

        public MonikerComparer(Dictionary<string, (string, int)> monikerOrder)
        {
            _monikerOrder = monikerOrder;
        }

        public int Compare(string x, string y)
        {
            Debug.Assert(_monikerOrder.ContainsKey(x) && _monikerOrder.ContainsKey(y));

            var (productNameX, orderXInProduct) = _monikerOrder[x];
            var (productNameY, orderYInProduct) = _monikerOrder[y];
            if (productNameX != productNameY)
            {
                // compare moniker name from different product alphabetically on product name
                return string.Compare(productNameX, productNameY);
            }

            return orderXInProduct.CompareTo(orderYInProduct);
        }
    }
}
