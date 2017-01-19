// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Xml;

    internal sealed class UidAndReader
    {
        public string CommentId { get; set; }
        public XmlReader Reader { get; set; }
        public UidAndComment ToUidAndElement() => new UidAndComment { CommentId = CommentId, Comment = Reader.ReadOuterXml() };
    }
}