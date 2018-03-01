// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger.Internals
{
    using System.Collections.Generic;
    using System.Linq;

    internal class SwaggerArray : SwaggerObjectBase
    {
        public override SwaggerObjectType ObjectType => SwaggerObjectType.Array;

        public List<SwaggerObjectBase> Array { get; set; } = new List<SwaggerObjectBase>();

        public override SwaggerObjectBase Clone()
        {
            var clone = (SwaggerArray)MemberwiseClone();
            clone.Array = Array.Select(a => a.Clone()).ToList();
            clone.ReferencesResolved = false;
            return clone;
        }
    }
}
