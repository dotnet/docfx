// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.SchemaDriven.Processors;

public class MergeTypeInterpreter : IInterpreter
{
    public int Order => 1;
    public bool CanInterpret(BaseSchema schema)
    {
        // TODO implement
        return false;
    }

    public object Interpret(BaseSchema schema, object value, IProcessContext context, string path)
    {
        // TODO implement
        return value;
    }

    private static object MergeCore(object value, IProcessContext context)
    {
        return value;
    }
}
