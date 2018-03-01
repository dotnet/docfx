// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger.Internals
{
    using Newtonsoft.Json.Linq;

    internal class SwaggerValue : SwaggerObjectBase
    {
        public override SwaggerObjectType ObjectType => SwaggerObjectType.ValueType;

        public JToken Token { get; set; }

        public override SwaggerObjectBase Clone()
        {
            var clone = (SwaggerValue)MemberwiseClone();
            clone.ReferencesResolved = false;
            return clone;
        }
    }
}
