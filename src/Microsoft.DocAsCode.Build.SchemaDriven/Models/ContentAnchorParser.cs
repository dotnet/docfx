// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    public class ContentAnchorParser : IContentAnchorParser
    {
        public const string AnchorContentName = "*content";

        public string Content { get; }

        public bool ContainsAnchor { get; private set; }

        public ContentAnchorParser(string content)
        {
            Content = content;
        }

        public string Parse(string input)
        {
            if (input != null && input.Trim() == AnchorContentName)
            {
                ContainsAnchor = true;
                return Content;
            }

            return input;
        }
    }
}
