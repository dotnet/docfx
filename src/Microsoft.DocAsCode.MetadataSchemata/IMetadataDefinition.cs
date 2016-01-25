// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MetadataSchemata
{
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;

    public interface IMetadataDefinition
    {
        string Type { get; }
        bool IsMultiValued { get; }
        bool IsQueryable { get; }
        bool IsRequired { get; }
        bool IsVisible { get; }
        string DisplayName { get; }
        string QueryName { get; }
        List<JValue> ChoiceSet { get; }
        string Description { get; }
    }
}