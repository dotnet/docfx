// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal static class MergeConflict
{
    private enum State
    {
        Text,
        CodeBlock,
        IncomingText,
        OutgoingText,
    }

    public static Error? CheckMergeConflictMarker(string content, FilePath file)
    {
        var state = State.Text;
        var nextCharIsNewLine = true;
        var startLine = 1;
        var line = 1;

        for (var i = 0; i < content.Length - 1; i++)
        {
            if (content[i] == '\n')
            {
                line++;
                nextCharIsNewLine = true;
                continue;
            }
            if (!nextCharIsNewLine)
            {
                continue;
            }
            nextCharIsNewLine = false;

            if (string.Compare(content, i, "```", 0, "```".Length, StringComparison.Ordinal) == 0)
            {
                state = state == State.CodeBlock ? State.Text : State.CodeBlock;
            }
            else if (
                state == State.Text &&
                string.Compare(content, i, "<<<<<<<", 0, "<<<<<<<".Length, StringComparison.Ordinal) == 0)
            {
                startLine = line;
                state = State.IncomingText;
            }
            else if (
                state == State.IncomingText &&
                string.Compare(content, i, "=======", 0, "=======".Length, StringComparison.Ordinal) == 0)
            {
                state = State.OutgoingText;
            }
            else if (
                state == State.OutgoingText &&
                string.Compare(content, i, ">>>>>>>", 0, ">>>>>>>".Length, StringComparison.Ordinal) == 0)
            {
                return Errors.Content.MergeConflict(new SourceInfo(file, startLine, 1));
            }
        }

        return null;
    }
}
