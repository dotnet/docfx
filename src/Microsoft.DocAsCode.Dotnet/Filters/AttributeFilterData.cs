// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dotnet
{
    using System.Collections.Generic;

    internal class AttributeFilterData
    {
        public string Id { get; set; }

        public IEnumerable<string> ConstructorArguments { get; set; }

        public IDictionary<string, string> ConstructorNamedArguments { get; set; }
    }
}
