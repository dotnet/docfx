// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger.Internals
{
    internal abstract class SwaggerFormattedReferenceBase
    {
        public abstract SwaggerFormattedReferenceType Type { get; }

        public string Path { get; set; }

        public string Name { get; set; }
    }
}
