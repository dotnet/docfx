// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    public interface ITagInterpreter
    {
        int Order { get; }
        bool Matches(string tagName);
        object Interpret(string tagName, BaseSchema schema, object value, IProcessContext context, string path);
    }
}
