// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger.Internals
{
    internal class SwaggerFormattedReference
    {
        public SwaggerFormattedReferenceType Type { get; set; }

        public string ExternalFilePath { get; set; }

        public string Path { get; set; }

        public string Name { get; set; }
    }

    internal enum SwaggerFormattedReferenceType
    {
        InternalReference,
        ExternalEmbeddedReference,
        ExternalReference
    }
}
