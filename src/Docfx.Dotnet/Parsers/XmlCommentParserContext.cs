// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.DataContracts.Common;

namespace Docfx.Dotnet;

internal class XmlCommentParserContext
{
    public bool SkipMarkup { get; init; }

    public Action<string, string> AddReferenceDelegate { get; init; }

    public Func<string, string> ResolveCode { get; init; }

    /// <summary>
    /// Resolves the assembly component of the UID of the API a cref points to, or <see langword="null"/>
    /// when that API isn't qualified by an assembly.
    /// </summary>
    public Func<string, string> ResolveAssemblyUid { get; init; }

    public SourceDetail Source { get; init; }
}
