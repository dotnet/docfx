// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
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

        private object MergeCore(object value, IProcessContext context)
        {
            return value;
        }
    }

}
