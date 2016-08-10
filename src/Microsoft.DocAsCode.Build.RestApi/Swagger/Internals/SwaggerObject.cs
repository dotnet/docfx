// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger.Internals
{
    using System.Collections.Generic;
    using System.Linq;

    internal class SwaggerObject : SwaggerObjectBase
    {
        public override SwaggerObjectType ObjectType => SwaggerObjectType.Object;

        public Dictionary<string, SwaggerObjectBase> Dictionary { get; set; } = new Dictionary<string, SwaggerObjectBase>();

        public override SwaggerObjectBase Clone()
        {
            var clone = (SwaggerObject)MemberwiseClone();
            clone.Dictionary = Dictionary.ToDictionary(k => k.Key, k => k.Value.Clone());
            clone.ReferencesResolved = false;
            return clone;
        }
    }
}
