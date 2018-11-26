// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
}
