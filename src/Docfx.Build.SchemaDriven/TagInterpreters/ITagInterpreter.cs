// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.SchemaDriven.Processors;

public interface ITagInterpreter
{
    int Order { get; }
    bool Matches(string tagName);
    object Interpret(string tagName, BaseSchema schema, object value, IProcessContext context, string path);
}
