// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    public interface IInterpreter
    {
        bool CanInterpret(BaseSchema schema);
        object Interpret(BaseSchema schema, object value, IProcessContext context, string path);
    }
}
