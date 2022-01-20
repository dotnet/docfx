// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Microsoft.Docs.Build;

internal class JsonSnakeCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var sb = new StringBuilder();
        var state = SnakeCaseState.Start;

        var nameSpan = name.AsSpan();

        for (var i = 0; i < nameSpan.Length; i++)
        {
            if (nameSpan[i] == ' ')
            {
                if (state != SnakeCaseState.Start)
                {
                    state = SnakeCaseState.NewWord;
                }
            }
            else if (char.IsUpper(nameSpan[i]))
            {
                switch (state)
                {
                    case SnakeCaseState.Upper:
                        var hasNext = i + 1 < nameSpan.Length;
                        if (i > 0 && hasNext)
                        {
                            var nextChar = nameSpan[i + 1];
                            if (!char.IsUpper(nextChar) && nextChar != '_')
                            {
                                sb.Append('_');
                            }
                        }
                        break;
                    case SnakeCaseState.Lower:
                    case SnakeCaseState.NewWord:
                        sb.Append('_');
                        break;
                }
                sb.Append(char.ToLowerInvariant(nameSpan[i]));
                state = SnakeCaseState.Upper;
            }
            else if (nameSpan[i] == '_')
            {
                sb.Append('_');
                state = SnakeCaseState.Start;
            }
            else
            {
                if (state == SnakeCaseState.NewWord)
                {
                    sb.Append('_');
                }

                sb.Append(nameSpan[i]);
                state = SnakeCaseState.Lower;
            }
        }

        return sb.ToString();
    }

    private enum SnakeCaseState
    {
        Start,
        Lower,
        Upper,
        NewWord,
    }
}
