// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;

    public class MarkdownException : Exception
    {
        public MarkdownException()
        {
        }

        public MarkdownException(string message) : base(message)
        {
        }

        public MarkdownException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class MarkdownParsingException : MarkdownException
    {
        public SourceInfo SourceInfo { get; private set; }

        public MarkdownParsingException(SourceInfo sourceInfo)
            : this("Unable to parse markdown", sourceInfo)
        {
        }

        public MarkdownParsingException(string message, SourceInfo sourceInfo) : base(GetMessage(message, sourceInfo))
        {
        }

        public MarkdownParsingException(string message, SourceInfo sourceInfo, Exception innerException) : base(GetMessage(message, sourceInfo), innerException)
        {
        }

        private static string GetMessage(string message, SourceInfo sourceInfo)
        {
            StringBuffer sb = message;
            if (sourceInfo.File != null)
            {
                sb = sb + " in " + sourceInfo.File;
            }
            if (sourceInfo.LineNumber > 0)
            {
                sb = sb + " at line " + sourceInfo.LineNumber.ToString();
            }
            sb += " with following markdown content:";
            sb += Environment.NewLine;
            var md = sourceInfo.Markdown;
            if (md.Length > 256)
            {
                md = md.Remove(256);
            }
            foreach (var line in md.Split('\n'))
            {
                sb = sb + "> " + line;
            }
            return sb.ToString();
        }
    }
}
