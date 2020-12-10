// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal abstract class HierarchyModel
    {
        public string? Uid { get; set; }

        public SourceInfo<string>? Source { get; set; }

        public string? ParentUid { get; set; }

        public List<string?> ChildrenUids { get; set; } = new List<string?>();

        public bool? UseAzureSandbox { get; set; }

        public string? SchemaType { get; set; }
    }
}
