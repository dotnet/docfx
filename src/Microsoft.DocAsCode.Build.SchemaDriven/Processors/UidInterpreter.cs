// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using System;

    using Newtonsoft.Json.Schema;

    using Microsoft.DocAsCode.Plugins;

    public class UidInterpreter : IInterpreter
    {
        public bool CanInterpret(BaseSchema schema)
        {
            return schema.ContentType == ContentType.Uid && schema.Type == JSchemaType.String;
        }

        public object Interpret(BaseSchema schema, object value, IProcessContext context, string path)
        {
            if (!CanInterpret(schema) || value == null)
            {
                return value;
            }

            if (!(value is string val))
            {
                throw new ArgumentException($"{value.GetType()} is not supported type string.");
            }

            context.Properties.Uids.Add(new UidDefinition(val, context.Model.LocalPathFromRoot, path: path));
            context.Properties.XrefSpecs.Add(new XRefSpec
            {
                Uid = val,
                // TODO: add from uidResolver
            });
            return value;
        }
    }
}
