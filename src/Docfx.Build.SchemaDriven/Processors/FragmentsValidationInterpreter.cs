// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.OverwriteDocuments;
using Docfx.Common;
using Markdig.Syntax;

namespace Docfx.Build.SchemaDriven.Processors;

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

        var markdownDocument = value as MarkdownDocument;
        if (markdownDocument != null && (schema?.ContentType != ContentType.Markdown))
        {
            Logger.LogWarning(
                $"There is an invalid H2: `{markdownDocument.GetData(Constants.OPathStringDataName)}`: the contentType of this property in schema must be `markdown`",
                line: markdownDocument.GetData(Constants.OPathLineNumberDataName)?.ToString(),
                code: WarningCodes.Overwrite.InvalidMarkdownFragments);
            return value;
        }

        if (schema == null)
        {
            return value;
        }

        if (markdownDocument == null && schema.ContentType == ContentType.Markdown)
        {
            Logger.LogWarning(
            $"Markdown property `{path.Trim('/')}` is not allowed inside a YAML code block",
            code: WarningCodes.Overwrite.InvalidMarkdownFragments);
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

        // TODO: improve error message by including line number and OPathString for YAML code block
        Logger.LogWarning(
            $"You cannot overwrite a readonly property: `{path.Trim('/')}`, please add an `editable` tag on this property or mark its contentType as `markdown` in schema if you want to overwrite this property",
            code: WarningCodes.Overwrite.InvalidMarkdownFragments);

        return value;
    }
}
