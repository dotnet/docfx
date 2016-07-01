// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger.Internals
{
    using Microsoft.DocAsCode.Build.Common;

    internal abstract class SwaggerObjectBase: ICloneable<SwaggerObjectBase>
    {
        public abstract SwaggerObjectType ObjectType { get; }

        public bool ReferencesResolved { get; set; }

        public abstract SwaggerObjectBase Clone();
    }
}
