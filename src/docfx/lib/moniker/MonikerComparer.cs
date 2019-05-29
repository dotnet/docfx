// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class MonikerComparer : IComparer<string>, IEqualityComparer<string>
    {
        private readonly Dictionary<string, (string productName, int orderInProduct)> _monikerOrder;

        public MonikerComparer(Dictionary<string, (string, int)> monikerOrder)
        {
            _monikerOrder = monikerOrder;
        }

        public int Compare(string x, string y)
        {
            int result;
            if (x is null || !_monikerOrder.TryGetValue(x, out var orderX))
            {
                result = -1;
            }
            else if (y is null || !_monikerOrder.TryGetValue(y, out var orderY))
            {
                result = 1;
            }
            else if (orderX.productName != orderY.productName)
            {
                // irrelevant comparison between different products
                result = int.MinValue;
            }
            else
            {
                result = orderX.orderInProduct.CompareTo(orderY.orderInProduct);
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
    }
}
