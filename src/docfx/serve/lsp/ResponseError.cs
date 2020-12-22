// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    public record ResponseError
    {
        public int? Code { get; init; }

        public string? Message { get; init; }
    }
}
