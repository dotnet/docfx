// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Exceptions;

public class SchemaKeywordNotSupportedException : DocfxException
{
    public SchemaKeywordNotSupportedException(string keyword) : base($"{keyword} keyword is not supported in current schema driven document processor")
    {
    }
}
