// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.SchemaDriven;

public static class SchemaExtensions
{
    private const string IsEditableTag = "editable";

    /// <summary>
    /// Return if a property is required to appear in markdown fragments
    /// </summary>
    public static bool IsRequiredInFragments(this BaseSchema schema) => IsEditable(schema);

    /// <summary>
    /// Return if a property is legal to appear in markdown fragments
    /// </summary>
    public static bool IsLegalInFragments(this BaseSchema schema) => IsEditable(schema);

    private static bool IsEditable(this BaseSchema schema)
    {
        if (schema?.ContentType == ContentType.Markdown)
        {
            return true;
        }
        if (schema?.Tags == null)
        {
            return false;
        }

        return schema.Tags.Contains(IsEditableTag, StringComparer.OrdinalIgnoreCase);
    }
}
