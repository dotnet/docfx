// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class BuildSpec
    {
        public Dictionary<string, string> Inputs { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> Outputs { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, DependencySpec> Dependencies { get; } = new Dictionary<string, DependencySpec>(StringComparer.OrdinalIgnoreCase);

        public class DependencySpec : Dictionary<string, string>
        {
            public DependencySpec() : base(StringComparer.OrdinalIgnoreCase) { }
        }
    }
}
