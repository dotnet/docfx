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
        private readonly IDictionary<JsonLocationInfo, SwaggerObjectBase> _documentObjectCache;
        private readonly IDictionary<JsonLocationInfo, SwaggerObjectBase> _resolvedObjectCache;
        private const string ReferenceKey = "$ref";
        private const string InternalRefNameKey = "x-internal-ref-name";
        private const string InternalLoopRefNameKey = "x-internal-loop-ref-name";
        private const string InternalLoopTokenKey = "x-internal-loop-token";


        public SwaggerJsonBuilder()
        {
            _documentObjectCache = new Dictionary<JsonLocationInfo, SwaggerObjectBase>();
            _resolvedObjectCache = new Dictionary<JsonLocationInfo, SwaggerObjectBase>();
        }

        public SwaggerObjectBase Read(string swaggerPath)
        {
            var swagger = Load(swaggerPath);
            return ResolveReferences(swagger, swaggerPath, new Stack<JsonLocationInfo>());
        }

        private SwaggerObjectBase Load(string swaggerPath)
        {
            using (JsonReader reader = new JsonTextReader(EnvironmentContext.FileAbstractLayer.OpenReadText(swaggerPath)))
            {
                reader.DateParseHandling = DateParseHandling.None;
                var token = JToken.ReadFrom(reader);
                return LoadCore(token, swaggerPath);
            }
        }

        private SwaggerObjectBase LoadCore(JToken token, string swaggerPath)
        {
            // Fetch from cache first
            var location = JsonLocationHelper.GetLocation(token);
            var jsonLocationInfo = new JsonLocationInfo(swaggerPath, location);

            if (_documentObjectCache.TryGetValue(jsonLocationInfo, out SwaggerObjectBase existingObject))
            {
                return existingObject;
            }

            if (token is JObject jObject)
            {
                // Only one $ref is allowed inside a swagger JObject
                if (jObject.TryGetValue(ReferenceKey, out JToken referenceToken))
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
                            _documentObjectCache.Add(jsonLocationInfo, deferredObject);
                            return deferredObject;
                        case SwaggerFormattedReferenceType.ExternalReference:
                            jObject.Remove("$ref");

                            var externalJObject = LoadExternalReference(Path.Combine(Path.GetDirectoryName(swaggerPath), swaggerReference.ExternalFilePath));
                            RestApiHelper.CheckSpecificKey(externalJObject, ReferenceKey, () =>
                            {
                                throw new DocfxException($"{ReferenceKey} in {swaggerReference.ExternalFilePath} is not supported in external reference currently.");
                            });
                            foreach (var item in externalJObject)
                            {
                                if (jObject.TryGetValue(item.Key, out JToken value))
                                {
                                    Logger.LogWarning($"{item.Key} inside {jObject.Path} would be overwritten by the value of same key inside {swaggerReference.ExternalFilePath} with path {externalJObject.Path}.");
                                }
                                jObject[item.Key] = item.Value;
                            }

                            var resolved = new SwaggerValue
                            {
                                Location = location,
                                Token = jObject
                            };
                            _documentObjectCache.Add(jsonLocationInfo, resolved);
                            return resolved;
                        case SwaggerFormattedReferenceType.ExternalEmbeddedReference:
                            // Defer resolving external reference to resolve step, to prevent loop reference.
                            var externalDeferredObject = new SwaggerReferenceObject
                            {
                                ExternalFilePath = Path.Combine(Path.GetDirectoryName(swaggerPath), swaggerReference.ExternalFilePath),
                                DeferredReference = swaggerReference.Path,
                                ReferenceName = swaggerReference.Name,
                                Location = location
                            };
                            jObject.Remove("$ref");
                            externalDeferredObject.Token = jObject;
                            _documentObjectCache.Add(jsonLocationInfo, externalDeferredObject);
                            return externalDeferredObject;
                        default:
                            throw new DocfxException($"{referenceToken} does not support type {swaggerReference.Type}.");
                    }
                }

                var swaggerObject = new SwaggerObject { Location = location };
                foreach (KeyValuePair<string, JToken> property in jObject)
                {
                    swaggerObject.Dictionary.Add(property.Key, LoadCore(property.Value, swaggerPath));
                }

                _documentObjectCache.Add(jsonLocationInfo, swaggerObject);
                return swaggerObject;
            }

            if (token is JArray jArray)
            {
                var swaggerArray = new SwaggerArray { Location = location };
                foreach (var property in jArray)
                {
                    swaggerArray.Array.Add(LoadCore(property, swaggerPath));
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

        private SwaggerObjectBase ResolveReferences(SwaggerObjectBase swaggerBase, string swaggerPath, Stack<JsonLocationInfo> refStack)
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

                            var jsonLocationInfo = new JsonLocationInfo(swagger.ExternalFilePath ?? swaggerPath, swagger.DeferredReference);
                            if (!_documentObjectCache.TryGetValue(jsonLocationInfo, out SwaggerObjectBase referencedObjectBase))
                            {
                                if (swagger.ExternalFilePath == null)
                                {
                                    throw new JsonException($"Could not resolve reference '{swagger.DeferredReference}' in the document.");
                                }

                                // Load external swagger, to fill in the document cache.
                                Load(swagger.ExternalFilePath);
                                if (!_documentObjectCache.TryGetValue(jsonLocationInfo, out referencedObjectBase))
                                {
                                    throw new JsonException($"Could not resolve reference '{swagger.DeferredReference}' in the document.");
                                }
                            }

                            if (refStack.Contains(jsonLocationInfo))
                            {
                                var loopRef = new SwaggerLoopReferenceObject();
                                loopRef.Dictionary.Add(InternalLoopRefNameKey, new SwaggerValue { Token = swagger.ReferenceName });
                                loopRef.Dictionary.Add(InternalLoopTokenKey, new SwaggerValue { Token = swagger.Token });
                                return loopRef;
                            }

                            // Clone to avoid change the reference object in _documentObjectCache
                            refStack.Push(jsonLocationInfo);

                            if (!_resolvedObjectCache.TryGetValue(jsonLocationInfo, out var resolvedObject))
                            {
                                resolvedObject = ResolveReferences(referencedObjectBase.Clone(), jsonLocationInfo.FilePath, refStack);
                                _resolvedObjectCache.Add(jsonLocationInfo, resolvedObject);
                            }

                            var swaggerObject = ResolveSwaggerObject(resolvedObject);
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
                            swagger.Dictionary[key] = ResolveReferences(swagger.Dictionary[key], swaggerPath, refStack);
                        }
                        return swagger;
                    }
                case SwaggerObjectType.Array:
                    {
                        var swagger = (SwaggerArray)swaggerBase;
                        for (int i = 0; i < swagger.Array.Count; i++)
                        {
                            swagger.Array[i] = ResolveReferences(swagger.Array[i], swaggerPath, refStack);
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
            if (swaggerObjectBase is SwaggerObject swaggerObject)
            {
                return swaggerObject;
            }

            if (swaggerObjectBase is SwaggerReferenceObject swaggerReferenceObject)
            {
                return swaggerReferenceObject.Reference;
            }

            throw new ArgumentException($"When resolving reference for {nameof(SwaggerReferenceObject)}, only support {nameof(SwaggerObject)} and {nameof(SwaggerReferenceObject)} as parameter.");
        }
    }
}
