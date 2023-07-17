// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.SchemaDriven.Processors;

public interface IInterpreter
{
    bool CanInterpret(BaseSchema schema);
    object Interpret(BaseSchema schema, object value, IProcessContext context, string path);
}
