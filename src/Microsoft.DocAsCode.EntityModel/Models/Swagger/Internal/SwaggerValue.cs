// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Swagger.Internal
{
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;

    internal class SwaggerValue : SwaggerObjectBase
    {
        public override SwaggerObjectType ObjectType => SwaggerObjectType.ValueType;
        public JToken Token { get; set; }
    }
}
