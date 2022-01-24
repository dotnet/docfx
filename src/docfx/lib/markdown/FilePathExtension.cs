// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Syntax;
using Microsoft.Docs.MarkdigExtensions;

namespace Microsoft.Docs.Build;

internal static class FilePathExtension
{
    private static readonly List<object?> s_emptyInclusionStack = new();
    private static readonly object s_filePathKey = new();

    public static MarkdownPipelineBuilder UseFilePath(this MarkdownPipelineBuilder builder)
    {
        return builder.Use(document => document.SetData(s_filePathKey, InclusionContext.File));
    }

    public static FilePath GetFilePath(this MarkdownObject obj)
    {
        foreach (var item in obj.GetPathToRootInclusive())
        {
            var file = item.GetData(s_filePathKey);
            if (file != null)
            {
                return ((SourceInfo)file).File;
            }
        }

        throw new InvalidOperationException();
    }

    public static SourceInfo GetSourceInfo(this MarkdownObject obj, int? line = null)
    {
        foreach (var item in obj.GetPathToRootInclusive())
        {
            if (item.GetData(s_filePathKey) is SourceInfo file)
            {
                if (line != null)
                {
                    return new SourceInfo(file.File, line.Value + 1, 0);
                }

                // Line info in markdown object is zero based, turn it into one based.
                return file.WithOffset(obj.Line + 1, obj.Column + 1);
            }
        }

        throw new InvalidOperationException();
    }

    public static bool IsInclude(this MarkdownObject obj)
    {
        foreach (var item in obj.GetPathToRootExclusive())
        {
            if (item is InclusionBlock || item is InclusionInline)
            {
                return true;
            }
        }
        return false;
    }

    public static List<object?> GetInclusionStack(this MarkdownObject obj)
    {
        var source = default((int line, int column)?);
        var result = default(List<object?>);

        foreach (var item in obj.GetPathToRootExclusive())
        {
            switch (item)
            {
                case InclusionBlock:
                case InclusionInline:
                    source = (item.Line + 1, item.Column + 1);
                    break;
            }

            if (source != null)
            {
                var file = item.GetData(s_filePathKey);
                if (file != null)
                {
                    result ??= new List<object?>();
                    result.Insert(0, ((SourceInfo)file).WithOffset(source.Value.line, source.Value.column));
                    source = null;
                }
            }
        }

        return result ?? s_emptyInclusionStack;
    }

    public static void SetSourceInfo(this MarkdownObject obj, SourceInfo value)
    {
        obj.SetData(s_filePathKey, value);
    }
}
