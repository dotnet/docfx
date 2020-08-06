// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Syntax;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class FilePathExtension
    {
        private static readonly object s_filePathKey = new object();

        public static MarkdownPipelineBuilder UseFilePath(this MarkdownPipelineBuilder builder)
        {
            return builder.Use(document => document.SetData(s_filePathKey, InclusionContext.File));
        }

        public static Document GetFilePath(this MarkdownObject? obj)
        {
            if (obj != null)
            {
                foreach (var item in obj.GetPathToRoot())
                {
                    var file = item.GetData(s_filePathKey);
                    if (file != null)
                    {
                        return (Document)file;
                    }
                }
            }

            return (Document)InclusionContext.File;
        }

        public static SourceInfo? GetSourceInfo(this MarkdownObject? obj, int? line = null)
        {
            var path = GetFilePath(obj).FilePath;

            if (line != null)
            {
                return new SourceInfo(path, line.Value + 1, 0);
            }

            if (obj is null)
            {
                return new SourceInfo(path, 0, 0);
            }

            // Line info in markdown object is zero based, turn it into one based.
            return new SourceInfo(path, obj.Line + 1, obj.Column + 1);
        }

        public static SourceInfo? GetSourceInfo(this MarkdownObject obj, in HtmlTextRange html)
        {
            var path = GetFilePath(obj).FilePath;

            var start = OffSet(obj.Line, obj.Column, html.Start.Line, html.Start.Column);
            var end = OffSet(obj.Line, obj.Column, html.End.Line, html.End.Column);

            return new SourceInfo(path, start.line + 1, start.column + 1, end.line + 1, end.column + 1);

            static (int line, int column) OffSet(int line1, int column1, int line2, int column2)
            {
                return line2 == 0 ? (line1, column1 + column2) : (line1 + line2, column2);
            }
        }

        public static void SetFilePath(this MarkdownObject obj, Document value)
        {
            obj.SetData(s_filePathKey, value);
        }
    }
}
