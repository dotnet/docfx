// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using System;
    using System.Collections.Generic;

    using Markdig.Syntax;

    using Microsoft.DocAsCode.Common;

    public class FragmentsValidationInterpreter : IInterpreter
    {
        public bool CanInterpret(BaseSchema schema)
        {
            return schema == null || schema.ContentType != ContentType.Uid;
        }

        public object Interpret(BaseSchema schema, object value, IProcessContext context, string path)
        {
            if (value is IDictionary<string, object> || value is IDictionary<object, object> || value is IList<object>)
            {
                return value;
            }

            if (schema?.MergeType == MergeType.Key)
            {
                return value;
            }

            if (schema?.Tags != null && schema.IsEditable())
            {
                return value;
            }

            if (!(value is MarkdownDocument) && schema == null)
            {
                return value;
            }

            // TODO: improve error message by including line number and OPathString
            Logger.LogWarning(
                $"You cannot overwrite a readonly property: {path}, please add an `editable` tag on this property in schema if you want to overwrite this property",
                code: WarningCodes.Overwrite.InvalidMarkdownFragments);

            return value;
        }
    }
}
