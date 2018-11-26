// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    public class CodeSnippetTag
    {
        public CodeSnippetTag(string name, int line, CodeSnippetTagType type)
        {
            Name = name;
            Line = line;
            Type = type;
        }

        public string Name { get; }

        public int Line { get; }

        public CodeSnippetTagType Type { get; }
    }

    public enum CodeSnippetTagType
    {
        Start,
        End
    }
}