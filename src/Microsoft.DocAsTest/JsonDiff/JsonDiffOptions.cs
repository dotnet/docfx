// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsTest
{
    public class JsonDiffOptions
    {
        public static JsonDiffOptions Default { get; } = new JsonDiffOptions();

        public bool AdditionalProperties { get; }

        public JsonDiffOptions(bool additionalProperties = true)
        {
            AdditionalProperties = additionalProperties;
        }
    }
}
