// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MetadataSchemata
{
    using System.Collections.Generic;
    using System.Linq;

    public class ValidationResults
    {
        public ValidationResults(IEnumerable<ValidationResult> results)
        {
            Items.AddRange(from r in results where !r.IsSuccess select r);
        }

        public bool IsSuccess => Items.Count == 0;
        public List<ValidationResult> Items { get; } = new List<ValidationResult>();
    }
}
