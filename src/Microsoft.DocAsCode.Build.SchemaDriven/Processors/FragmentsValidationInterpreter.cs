// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    public class FragmentsValidationInterpreter : IInterpreter
    {
        public bool CanInterpret(BaseSchema schema)
        {
            return schema == null || schema.ContentType != ContentType.Uid;
        }

        public object Interpret(BaseSchema schema, object value, IProcessContext context, string path)
        {
            if (value is IDictionary<string, Object> || value is IDictionary<object, Object> || value is IList<object>)
            {
                return value;
            }

            if (schema?.MergeType == MergeType.Key)
            {
                return value;
            }

            if (schema?.Tags != null && schema.IsLegalInFragments())
            {
                return value;
            }

            if (value is string && schema == null)
            {
                return value;
            }

            // TODO: improve error message by including line number and OPathString
            Logger.LogWarning(
                $"You cannot overwrite an uneditable property: {path}, please add an `editable` tag on this property in schema if you want to overwrite this property",
                code: WarningCodes.Fragments.OverwriteUneditableProperty);

            return value;
        }
    }
}
