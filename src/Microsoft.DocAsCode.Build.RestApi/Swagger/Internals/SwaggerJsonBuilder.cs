// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger.Internals
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class SwaggerJsonBuilder
    {
        private IDictionary<string, SwaggerObjectBase> _documentObjectCache;
        private const string DefinitionsKey = "definitions";
        private const string ReferenceKey = "$ref";
        private const string ParametersKey = "parameters";
        private const string InternalRefNameKey = "x-internal-ref-name";
        private const string InternalLoopRefNameKey = "x-internal-loop-ref-name";

        public SwaggerObjectBase Read(string swaggerPath)
        {
            using (JsonReader reader = new JsonTextReader(EnvironmentContext.FileAbstractLayer.OpenReadText(swaggerPath)))
            {
                _documentObjectCache = new Dictionary<string, SwaggerObjectBase>();
                var token = JToken.ReadFrom(reader);

                var swaggerDir = Path.GetDirectoryName(swaggerPath);
                if (string.IsNullOrEmpty(swaggerDir))
                {
                    throw new DocfxException($"Directory of swagger file path {swaggerPath} should not be null or empty.");
                }
                var swagger = Build(token, swaggerDir);
                RemoveReferenceDefinitions((SwaggerObject)swagger);
                return ResolveReferences(swagger, new Stack<string>());
            }
        }

        private SwaggerObjectBase Build(JToken token, string swaggerDir)
        {
            // Fetch from cache first
            var location = JsonLocationHelper.GetLocation(token);
            SwaggerObjectBase existingObject;
            if (_documentObjectCache.TryGetValue(location, out existingObject))
            {
                return existingObject;
            }

            var jObject = token as JObject;
            if (jObject != null)
            {
                // Only one $ref is allowed inside a swagger JObject
                JToken referenceToken;
                if (jObject.TryGetValue(ReferenceKey, out referenceToken))
                {
                    if (referenceToken.Type != JTokenType.String && referenceToken.Type != JTokenType.Null)
                    {
                        throw new JsonException($"JSON reference $ref property must have a string or null value, instead of {referenceToken.Type}, location: {referenceToken.Path}.");
                    }

                    var swaggerReference = RestApiHelper.FormatReferenceFullPath((string)referenceToken);
                    switch (swaggerReference.Type)
                    {
                        case SwaggerFormattedReferenceType.InternalReference:
                            var deferredObject = new SwaggerReferenceObject
                            {
                                DeferredReference = swaggerReference.Path,
                                ReferenceName = swaggerReference.Name,
                                Location = location
                            };

                            // For swagger, other properties are still allowed besides $ref, e.g.
                            // "schema": {
                            //   "$ref": "#/definitions/foo"
                            //   "example": { }
                            // }
                            // Use Token property to keep other properties
                            // These properties cannot be referenced
                            jObject.Remove("$ref");
                            deferredObject.Token = jObject;
                            _documentObjectCache.Add(location, deferredObject);
                            return deferredObject;
                        case SwaggerFormattedReferenceType.ExternalReference:
                            jObject.Remove("$ref");

                            var externalJObject = LoadExternalReference(Path.Combine(swaggerDir, swaggerReference.Path));
                            RestApiHelper.CheckSpecificKey(externalJObject, ReferenceKey, () =>
                            {
                                throw new DocfxException($"{ReferenceKey} in {swaggerReference.Path} is not supported in external reference currently.");
                            });
                            foreach (var item in externalJObject)
                            {
                                JToken value;
                                if (jObject.TryGetValue(item.Key, out value))
                                {
                                    Logger.LogWarning($"{item.Key} inside {jObject.Path} would be overwritten by the value of same key inside {swaggerReference.Path} with path {externalJObject.Path}.");
                                }
                                jObject[item.Key] = item.Value;
                            }

                            return new SwaggerValue
                            {
                                Location = location,
                                Token = jObject
                            };
                        default:
                            throw new DocfxException($"{referenceToken} does not support type {swaggerReference.Type}.");
                    }
                }

                var swaggerObject = new SwaggerObject { Location = location };
                foreach (KeyValuePair<string, JToken> property in jObject)
                {
                    swaggerObject.Dictionary.Add(property.Key, Build(property.Value, swaggerDir));
                }

                _documentObjectCache.Add(location, swaggerObject);
                return swaggerObject;
            }

            var jArray = token as JArray;
            if (jArray != null)
            {
                var swaggerArray = new SwaggerArray { Location = location };
                foreach (var property in jArray)
                {
                    swaggerArray.Array.Add(Build(property, swaggerDir));
                }

                return swaggerArray;
            }

            return new SwaggerValue
            {
                Location = location,
                Token = token
            };
        }

        private static JObject LoadExternalReference(string externalSwaggerPath)
        {
            if (!EnvironmentContext.FileAbstractLayer.Exists(externalSwaggerPath))
            {
                throw new DocfxException($"External swagger path not exist: {externalSwaggerPath}.");
            }
            using (JsonReader reader = new JsonTextReader(EnvironmentContext.FileAbstractLayer.OpenReadText(externalSwaggerPath)))
            {
                return JObject.Load(reader);
            }
        }

        private static void RemoveReferenceDefinitions(SwaggerObject root)
        {
            // Remove definitions and parameters which has been added into _documentObjectCache
            if (root.Dictionary.ContainsKey(DefinitionsKey))
            {
                root.Dictionary.Remove(DefinitionsKey);
            }
            if (root.Dictionary.ContainsKey(ParametersKey))
            {
                root.Dictionary.Remove(ParametersKey);
            }
        }

        private SwaggerObjectBase ResolveReferences(SwaggerObjectBase swaggerBase, Stack<string> refStack)
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

                            SwaggerObjectBase referencedObjectBase;
                            if (!_documentObjectCache.TryGetValue(swagger.DeferredReference, out referencedObjectBase))
                            {
                                throw new JsonException($"Could not resolve reference '{swagger.DeferredReference}' in the document.");
                            }

                            if (refStack.Contains(referencedObjectBase.Location))
                            {
                                var loopRef = new SwaggerLoopReferenceObject();
                                loopRef.Dictionary.Add(InternalLoopRefNameKey, new SwaggerValue { Token = swagger.ReferenceName });
                                return loopRef;
                            }

                            // Clone to avoid change the reference object in _documentObjectCache
                            refStack.Push(referencedObjectBase.Location);
                            var resolved = ResolveReferences(referencedObjectBase.Clone(), refStack);
                            var swaggerObject = ResolveSwaggerObject(resolved);
                            if (!swaggerObject.Dictionary.ContainsKey(InternalRefNameKey))
                            {
                                swaggerObject.Dictionary.Add(InternalRefNameKey, new SwaggerValue { Token = swagger.ReferenceName });
                            }
                            swagger.Reference = swaggerObject;
                            refStack.Pop();
                        }
                        return swagger;
                    }
                case SwaggerObjectType.Object:
                    {
                        var swagger = (SwaggerObject)swaggerBase;
                        foreach (var key in swagger.Dictionary.Keys.ToList())
                        {
                            swagger.Dictionary[key] = ResolveReferences(swagger.Dictionary[key], refStack);
                        }
                        return swagger;
                    }
                case SwaggerObjectType.Array:
                    {
                        var swagger = (SwaggerArray)swaggerBase;
                        for (int i = 0; i < swagger.Array.Count; i++)
                        {
                            swagger.Array[i] = ResolveReferences(swagger.Array[i], refStack);
                        }
                        return swagger;
                    }
                case SwaggerObjectType.ValueType:
                    return swaggerBase;
                default:
                    throw new NotSupportedException(swaggerBase.ObjectType.ToString());
            }
        }

        private static SwaggerObject ResolveSwaggerObject(SwaggerObjectBase swaggerObjectBase)
        {
            var swaggerObject = swaggerObjectBase as SwaggerObject;
            if (swaggerObject != null)
            {
                return swaggerObject;
            }

            var swaggerReferenceObject = swaggerObjectBase as SwaggerReferenceObject;
            if (swaggerReferenceObject != null)
            {
                return swaggerReferenceObject.Reference;
            }

            throw new ArgumentException($"When resolving reference for {nameof(SwaggerReferenceObject)}, only support {nameof(SwaggerObject)} and {nameof(SwaggerReferenceObject)} as parameter.");
        }
    }
}
