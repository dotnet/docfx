// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Swagger.Internal
{
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;

    internal class SwaggerArray : SwaggerObjectBase
    {
        public override SwaggerObjectType ObjectType => SwaggerObjectType.Array;

        public List<SwaggerObjectBase> Array { get; set; } = new List<SwaggerObjectBase>();
    }
}
