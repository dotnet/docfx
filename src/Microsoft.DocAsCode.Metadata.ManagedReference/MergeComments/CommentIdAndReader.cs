// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Xml;

    internal sealed class CommentIdAndReader
    {
        public string CommentId { get; set; }
        public XmlReader Reader { get; set; }
        public CommentIdAndComment ToCommentIdAndComment() => new CommentIdAndComment { CommentId = CommentId, Comment = Reader?.ReadOuterXml() };
    }
}