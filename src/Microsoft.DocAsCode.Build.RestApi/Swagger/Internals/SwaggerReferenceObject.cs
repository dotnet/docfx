// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger.Internals
{
    using Newtonsoft.Json.Linq;

    internal class SwaggerReferenceObject : SwaggerObjectBase
    {
        public override SwaggerObjectType ObjectType => SwaggerObjectType.ReferenceObject;

        public string DeferredReference { get; set; }

        public string ReferenceName { get; set; }

        public string ExternalFilePath { get; set; }

        public JObject Token { get; set; }

        public SwaggerObject Reference { get; set; }

        public override SwaggerObjectBase Clone()
        {
            var clone = (SwaggerReferenceObject)MemberwiseClone();
            clone.Reference = (SwaggerObject)Reference?.Clone();
            clone.ReferencesResolved = false;
            return clone;
        }
    }
}
