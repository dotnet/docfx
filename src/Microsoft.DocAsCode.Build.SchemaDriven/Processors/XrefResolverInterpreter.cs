// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using System;

    using Newtonsoft.Json.Schema;

    using Microsoft.DocAsCode.Plugins;
    using System.Linq;
    using Microsoft.DocAsCode.Common;

    public class XrefResolverInterpreter : IInterpreter
    {
        /// <summary>
        /// Report xrefSpec when
        /// 1. ContentType = uid is defined => internal xref spec
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

            if (!string.IsNullOrEmpty(schema.XrefResolver))
            {
                return schema.XrefResolver.StartsWith("uid");
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
                Logger.LogWarning($"Invalid xrefResolver for {path}: uid is not set.");
                return value;
            }

            var xrefSpec = new XRefSpec
            {
                Uid = uid
            };

            // parts to skip uid
            var parts = schema.XrefResolver?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Skip(1) ?? new string[] { "name" };
            var root = context.Model.Content;
            foreach (var part in parts.Distinct())
            {
                var jsonPointer = new JsonPointer(path + "/" + part);
                var property = jsonPointer.GetValue(root);
                if (property != null)
                {
                    xrefSpec[EncodeKey(part)] = property as string;
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

        /// <summary>
        /// Part encode: /a/b/0 => .a.b.0
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        private string EncodeKey(string part)
        {
            return part.Replace('/', '.');
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
