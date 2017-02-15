// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger.Internals
{
    internal class SwaggeraInternalFormattedReference : SwaggerFormattedReferenceBase
    {
        public override SwaggerFormattedReferenceType Type => SwaggerFormattedReferenceType.InternalReference;
    }
}