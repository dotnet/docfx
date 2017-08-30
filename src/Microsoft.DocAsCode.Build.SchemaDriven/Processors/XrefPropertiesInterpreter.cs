﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json.Schema;

    public class XrefPropertiesInterpreter : IInterpreter
    {
        /// <summary>
        /// Report xrefSpec when
        /// 1. ContentType = uid is defined => xref spec to be exported to xrefspec.yml
        /// Or 2. XrefResolver is defined => external xref spec
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public bool CanInterpret(BaseSchema schema)
        {
            if (schema.Type != JSchemaType.Object)
            {
                return false;
            }

            if (schema.XrefProperties != null)
            {
                return true;
            }

            if (schema.Properties == null)
            {
                return false;
            }

            if (schema.Properties.TryGetValue("uid", out var baseSchema))
            {
                return baseSchema.ContentType == ContentType.Uid;
            }

            return false;
        }

        public object Interpret(BaseSchema schema, object value, IProcessContext context, string path)
        {
            if (value == null || !CanInterpret(schema))
            {
                return value;
            }

            var uid = JsonPointer.GetChild(value, "uid") as string;
            if (string.IsNullOrEmpty(uid))
            {
                Logger.LogWarning($"Invalid xrefProperties for {path}: uid is not defined.");
                return value;
            }

            var xrefSpec = new XRefSpec
            {
                Uid = uid
            };

            var parts = schema.XrefProperties ?? new List<string> { "name", "fullName" };
            var root = context.Model.Content;
            foreach (var part in parts.Distinct())
            {
                var jsonPointer = new JsonPointer(path + "/" + part);
                var property = jsonPointer.GetValue(root);
                if (property != null)
                {
                    if (property is string str)
                    {
                        xrefSpec[part] = str;
                    }
                    else
                    {
                        Logger.LogWarning($"Type {property.GetType()} from {jsonPointer} is not supported as the value of xref spec.");
                    }
                }
            }

            if (IsInternalXrefSpec(schema))
            {
                context.Properties.Uids.Add(new UidDefinition(uid, context.Model.LocalPathFromRoot, path: path + "/uid"));
                xrefSpec.Href = ((RelativePath)context.Model.Key).UrlEncode().ToString();
                context.Properties.XRefSpecs.Add(xrefSpec);
            }
            else
            {
                context.Properties.ExternalXRefSpecs.Add(xrefSpec);
            }
            return value;
        }

        private bool IsInternalXrefSpec(BaseSchema schema)
        {
            if (schema.Properties == null)
            {
                return false;
            }

            if (schema.Properties.TryGetValue("uid", out var innerSchema) != true)
            {
                return false;
            }

            return innerSchema.ContentType == ContentType.Uid;
        }
    }
}
