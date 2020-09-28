// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Markdig;
using Markdig.Syntax;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class FilePathExtension
    {
        private static readonly List<object?> s_emptyInclusionStack = new List<object?>();
        private static readonly object s_filePathKey = new object();

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

        public static SourceInfo? GetSourceInfo(this MarkdownObject obj, int? line = null)
        {
            foreach (var item in obj.GetPathToRootInclusive())
            {
                var file = item.GetData(s_filePathKey);
                if (file != null)
                {
                    return ((SourceInfo)file).WithOffset(obj, line);
                }
            }

            throw new InvalidOperationException();
        }

        public static SourceInfo WithOffset(this SourceInfo sourceInfo, MarkdownObject? obj, int? line = null)
        {
            if (line != null)
            {
                return new SourceInfo(sourceInfo.File, line.Value + 1, 0);
            }

            if (obj is null)
            {
                return sourceInfo;
            }

            // Line info in markdown object is zero based, turn it into one based.
            return sourceInfo.WithOffset(obj.Line + 1, obj.Column + 1);
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
                    case InclusionBlock _:
                    case InclusionInline _:
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
}
