// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger.Internals
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class SwaggerJsonBuilder
    {
        private IDictionary<string, SwaggerObject> _documentObjectCache;

        public SwaggerObjectBase Read(JsonReader reader)
        {
            _documentObjectCache = new Dictionary<string, SwaggerObject>();
            var token = JToken.ReadFrom(reader);
            var swagger = Build(token);
            return ResolveReferences(swagger);
        }

        private SwaggerObjectBase Build(JToken token)
        {
            var jObject = token as JObject;
            if (jObject != null)
            {
                JToken referenceToken;

                // Only one $ref is allowed inside a swagger JObject
                if (jObject.TryGetValue("$ref", out referenceToken))
                {
                    if (referenceToken.Type != JTokenType.String && referenceToken.Type != JTokenType.Null)
                    {
                        throw new JsonException($"JSON reference $ref property must have a string or null value, instead of {referenceToken.Type}, location: {referenceToken.Path}.");
                    }

                    SwaggerReferenceObject deferredObject = new SwaggerReferenceObject();
                    deferredObject.DeferredReference = RestApiHelper.FormatReferenceFullPath((string)referenceToken);

                    // For swagger, other properties are still allowed besides $ref, e.g.
                    // "schema": {
                    //   "$ref": "#/defintions/foo"
                    //   "example": { }
                    // }
                    // Use Token property to keep other properties
                    // These properties cannot be referenced
                    jObject.Remove("$ref");
                    deferredObject.Token = jObject;
                    return deferredObject;
                }
                else
                {
                    string location = GetLocation(token);

                    SwaggerObject existingObject;
                    if (_documentObjectCache.TryGetValue(location, out existingObject))
                    {
                        return existingObject;
                    }

                    var swaggerObject = new SwaggerObject { Location = location };

                    foreach (KeyValuePair<string, JToken> property in jObject)
                    {
                        swaggerObject.Dictionary.Add(property.Key, Build(property.Value));
                    }

                    _documentObjectCache.Add(location, swaggerObject);
                    return swaggerObject;
                }
            }

            var jArray = token as JArray;
            if (jArray != null)
            {
                var swaggerArray = new SwaggerArray();
                foreach (var property in jArray)
                {
                    swaggerArray.Array.Add(Build(property));
                }

                return swaggerArray;
            }

            return new SwaggerValue
            {
                Token = token
            };
        }

        private SwaggerObjectBase ResolveReferences(SwaggerObjectBase swaggerBase)
        {
            if (swaggerBase.ReferencesResolved)
            {
                return swaggerBase;
            }

            swaggerBase.ReferencesResolved = true;
            switch (swaggerBase.ObjectType)
            {
                case SwaggerObjectType.ReferenceObject:
                    {
                        var swagger = (SwaggerReferenceObject)swaggerBase;
                        if (!string.IsNullOrEmpty(swagger.DeferredReference))
                        {
                            if (swagger.DeferredReference[0] != '/')
                            {
                                throw new JsonException($"reference \"{swagger.DeferredReference}\" is not supported. Reference must be inside current schema document starting with /");
                            }

                            SwaggerObject referencedObject;
                            if (!_documentObjectCache.TryGetValue(swagger.DeferredReference, out referencedObject))
                            {
                                throw new JsonException($"Could not resolve reference '{swagger.DeferredReference}' in the document.");
                            }

                            swagger.Reference = referencedObject;
                        }
                        return swagger;
                    }
                case SwaggerObjectType.Object:
                    {
                        var swagger = (SwaggerObject)swaggerBase;
                        foreach (var key in swagger.Dictionary.Keys.ToList())
                        {
                            swagger.Dictionary[key] = ResolveReferences(swagger.Dictionary[key]);
                        }
                        return swagger;
                    }
                case SwaggerObjectType.Array:
                    {
                        var swagger = (SwaggerArray)swaggerBase;
                        for (int i = 0; i < swagger.Array.Count; i++)
                        {
                            swagger.Array[i] = ResolveReferences(swagger.Array[i]);
                        }
                        return swagger;
                    }
                case SwaggerObjectType.ValueType:
                    return swaggerBase;
                default:
                    throw new NotSupportedException(swaggerBase.ObjectType.ToString());
            }
        }

        private static string GetLocation(JToken token)
        {
            if (token.Parent == null)
            {
                return "#";
            }

            IList<JToken> ancestors = token.AncestorsAndSelf().Reverse().ToList();

            var locations = new List<IJsonLocation>();
            for (int i = 0; i < ancestors.Count; i++)
            {
                JToken current = ancestors[i];
                switch (current.Type)
                {
                    case JTokenType.Property:
                        JProperty property = (JProperty)current;
                        locations.Add(new JsonObjectLocation(property.Name));
                        break;
                    case JTokenType.Array:
                    case JTokenType.Constructor:
                        if (i < ancestors.Count - 1)
                        {
                            var next = ancestors[i + 1];
                            int index = ((IList<JToken>)current).IndexOf(next);
                            locations.Add(new JsonIndexLocation(index));
                        }
                        break;
                }
            }

            StringBuilder sb = new StringBuilder();
            foreach (var state in locations)
            {
                state.WriteTo(sb);
            }

            return sb.ToString();
        }

        private class JsonIndexLocation : IJsonLocation
        {
            private readonly int _position;
            public JsonIndexLocation(int position)
            {
                _position = position;
            }

            public void WriteTo(StringBuilder sb)
            {
                sb.Append('/');
                sb.Append(_position);
            }
        }

        private class JsonObjectLocation : IJsonLocation
        {
            private readonly string _propertyName;
            public JsonObjectLocation(string propertyName)
            {
                _propertyName = propertyName;
            }

            public void WriteTo(StringBuilder sb)
            {
                sb.Append('/');
                sb.Append(RestApiHelper.FormatDefinitionSinglePath(_propertyName));
            }
        }

        private interface IJsonLocation
        {
            void WriteTo(StringBuilder sb);
        }
    }
}
