// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger.Internals
{
    internal class SwaggeraExternalFormattedReference : SwaggerFormattedReferenceBase
    {
        public override SwaggerFormattedReferenceType Type => SwaggerFormattedReferenceType.ExternalReference;

        public string ExternalFilePath { get; set; }
    }
}
