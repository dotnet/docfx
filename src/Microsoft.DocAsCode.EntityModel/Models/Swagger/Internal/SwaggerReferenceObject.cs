// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Swagger.Internal
{
    using Newtonsoft.Json.Linq;

    internal class SwaggerReferenceObject : SwaggerObjectBase
    {
        public override SwaggerObjectType ObjectType => SwaggerObjectType.ReferenceObject;
        public string DeferredReference { get; set; }
        public JObject Token { get; set; }
        public SwaggerObject Reference { get; set; }
    }
}
