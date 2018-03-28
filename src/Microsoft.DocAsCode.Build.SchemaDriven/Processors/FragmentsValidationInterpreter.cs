﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
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

            if ((value is MarkdownDocument) && (schema?.ContentType != ContentType.Markdown))
            {
                Logger.LogWarning(
                $"There is an invalid H2: {path}: the contentType of `{path}` in schema must be `markdown`",
                code: WarningCodes.Overwrite.InvalidMarkdownFragments);
            }

            if (schema == null)
            {
                return value;
            }

            if (schema.MergeType == MergeType.Key)
            {
                return value;
            }

            if (schema.IsLegalInFragments())
            {
                return value;
            }

            // TODO: improve error message by including line number and OPathString
            Logger.LogWarning(
                $"You cannot overwrite a readonly property: {path}, please add an `editable` tag on this property or mark its contentType as `markdown` in schema if you want to overwrite this property",
                code: WarningCodes.Overwrite.InvalidMarkdownFragments);

            return value;
        }
    }
}
