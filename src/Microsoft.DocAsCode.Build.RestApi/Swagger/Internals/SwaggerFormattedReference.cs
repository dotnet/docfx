// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi
{
    public class SwaggerFormattedReference
    {
        public SwaggerFormattedReferenceType Type { get; set; }

        public string Path { get; set; }

        public string Name { get; set; }
    }

    public enum SwaggerFormattedReferenceType
    {
        InternalReference,
        ExternalReference
    }
}
