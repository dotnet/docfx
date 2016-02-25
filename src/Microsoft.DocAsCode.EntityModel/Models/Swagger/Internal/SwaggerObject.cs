// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Swagger.Internal
{
    using System.Collections.Generic;

    internal class SwaggerObject : SwaggerObjectBase
    {
        public override SwaggerObjectType ObjectType => SwaggerObjectType.Object;

        public Dictionary<string, SwaggerObjectBase> Dictionary { get; set; } = new Dictionary<string, SwaggerObjectBase>();
        public string Location { get; set; }
    }
}
