// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors;

public class TagsInterpreter : IInterpreter
{
    private readonly IList<ITagInterpreter> _tagInterpreters;
    public int Order => 0x100;
    public TagsInterpreter(IList<ITagInterpreter> tagInterpreters)
    {
        _tagInterpreters = tagInterpreters;
    }

    public bool CanInterpret(BaseSchema schema)
    {
        return _tagInterpreters?.Count > 0 && schema?.Tags?.Count > 0;
    }

    public object Interpret(BaseSchema schema, object value, IProcessContext context, string path)
    {
        if (!CanInterpret(schema))
        {
            return value;
        }
        var val = value;

        foreach(var tag in schema.Tags)
        {
            foreach (var i in _tagInterpreters.Where(t => t.Matches(tag)).OrderBy(t => t.Order))
            {
                val = i.Interpret(tag, schema, val, context, path);
            }
        }

        return val;
    }
}
