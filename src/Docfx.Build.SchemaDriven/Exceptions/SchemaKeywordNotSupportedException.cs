// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Exceptions;

public class SchemaKeywordNotSupportedException : DocfxException
{
    public SchemaKeywordNotSupportedException(string keyword) : base($"{keyword} keyword is not supported in current schema driven document processor")
    {
    }
}
