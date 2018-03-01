// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    public class MarkdownWithContentAnchorInterpreter : IInterpreter
    {
        private readonly IInterpreter _inner;
        public MarkdownWithContentAnchorInterpreter(IInterpreter inner)
        {
            _inner = inner;
        }

        public bool CanInterpret(BaseSchema schema)
        {
            return true;
        }

        public object Interpret(BaseSchema schema, object value, IProcessContext context, string path)
        {
            if (value == null || !CanInterpret(schema) || !(value is string val))
            {
                return value;
            }

            // If *content is from an included file, it should be marked instead of consider it as a placeholder
            if ((schema == null || schema.Reference == ReferenceType.None) && context.ContentAnchorParser != null)
            {
                return context.ContentAnchorParser.Parse(val);
            }

            return _inner.Interpret(schema, value, context, path);
        }
    }
}
