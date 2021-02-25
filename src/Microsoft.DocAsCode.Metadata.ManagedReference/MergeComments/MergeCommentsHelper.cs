﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    public static class MergeCommentsHelper
    {
        public static void MergeComments(ExtractMetadataOptions options, MetadataItem item, IEnumerable<string> commentFiles)
        {
            if (item == null || commentFiles == null)
            {
                return;
            }

            var list = from file in commentFiles
                       let name = Path.GetFileNameWithoutExtension(file)
                       where string.Equals(name, item.Name, StringComparison.OrdinalIgnoreCase)
                       from uidAndReader in EnumerateDeveloperComments(file)
                       select uidAndReader.ToCommentIdAndComment();

            PatchMetadataItem(options, item, list);
            RebuildReference(item);
        }

        #region Private methods
        private static void RebuildReference(MetadataItem assembly)
        {
            var references = assembly.References;
            foreach (var namespaceItem in assembly.Items)
            {
                RebuildReference(namespaceItem, references);
            }
        }

        private static void RebuildReference(MetadataItem item, Dictionary<string, ReferenceItem> references)
        {
            if (item.Exceptions != null)
            {
                foreach (var exceptions in item.Exceptions)
                {
                    AddReference(exceptions.Type, exceptions.CommentId, references);
                }
            }

            if (item.Sees != null)
            {
                foreach (var i in item.Sees.Where(l => l.LinkType == LinkType.CRef))
                {
                    AddReference(i.LinkId, i.CommentId, references);
                }
            }

            if (item.SeeAlsos != null)
            {
                foreach (var i in item.SeeAlsos.Where(l => l.LinkType == LinkType.CRef))
                {
                    AddReference(i.LinkId, i.CommentId, references);
                }
            }

            foreach (var childItem in item.Items ?? Enumerable.Empty<MetadataItem>())
            {
                RebuildReference(childItem, references);
            }
        }

        private static void AddReference(string id, string commentId, Dictionary<string, ReferenceItem> references)
        {
            if (!references.TryGetValue(id, out ReferenceItem reference))
            {
                references[id] = new ReferenceItem { CommentId = commentId };
            }
        }

        private static void PatchMetadataItem(ExtractMetadataOptions options, MetadataItem assembly, IEnumerable<CommentIdAndComment> list)
        {
            var allItemsInAssembly = new Dictionary<string, MetadataItem>(StringComparer.OrdinalIgnoreCase);
            GetAllItemByCommentId(allItemsInAssembly, assembly);

            foreach (var uidAndComment in list)
            {
                MetadataItem item = null;
                if (allItemsInAssembly.TryGetValue(uidAndComment.CommentId, out item))
                {
                    PatchViewModel(options, item, uidAndComment.Comment);
                }
            }
        }

        private static void GetAllItemByCommentId(Dictionary<string, MetadataItem> items, MetadataItem item)
        {
            if (item.CommentId != null && !items.ContainsKey(item.CommentId))
            {
                items.Add(item.CommentId, item);
            }

            foreach (var metadataItem in item.Items ?? Enumerable.Empty<MetadataItem>())
            {
                GetAllItemByCommentId(items, metadataItem);
            }
        }

        private static void PatchViewModel(ExtractMetadataOptions options, MetadataItem item, string comment)
        {
            var context = new TripleSlashCommentParserContext
            {
                AddReferenceDelegate = (s, e) => { },
                CodeSourceBasePath = options.CodeSourceBasePath,
                
            };
            var commentModel = TripleSlashCommentModel.CreateModel(comment, SyntaxLanguage.CSharp, context);
            var summary = commentModel.Summary;
            if (!string.IsNullOrEmpty(summary))
            {
                item.Summary = summary;
            }
            var remarks = commentModel.Remarks;
            if (!string.IsNullOrEmpty(remarks))
            {
                item.Remarks = remarks;
            }
            var exceptions = commentModel.Exceptions;
            if (exceptions != null && exceptions.Count > 0)
            {
                item.Exceptions = exceptions;
            }
            var sees = commentModel.Sees;
            if (sees != null && sees.Count > 0)
            {
                item.Sees = sees;
            }
            var seeAlsos = commentModel.SeeAlsos;
            if (seeAlsos != null && seeAlsos.Count > 0)
            {
                item.SeeAlsos = seeAlsos;
            }
            var examples = commentModel.Examples;
            if (examples != null && examples.Count > 0)
            {
                item.Examples = examples;
            }
            if (item.Syntax != null)
            {
                if (item.Syntax.Parameters != null)
                {
                    foreach (var p in item.Syntax.Parameters)
                    {
                        var description = commentModel.GetParameter(p.Name);
                        if (!string.IsNullOrEmpty(description))
                        {
                            p.Description = description;
                        }
                    }
                }
                if (item.Syntax.TypeParameters != null)
                {
                    foreach (var p in item.Syntax.TypeParameters)
                    {
                        var description = commentModel.GetTypeParameter(p.Name);
                        if (!string.IsNullOrEmpty(description))
                        {
                            p.Description = description;
                        }
                    }
                }
                if (item.Syntax.Return != null)
                {
                    var returns = commentModel.Returns;
                    if (!string.IsNullOrEmpty(returns))
                    {
                        item.Syntax.Return.Description = returns;
                    }
                }
            }
            item.InheritDoc = commentModel.InheritDoc;
            // todo more.
        }

        private static IEnumerable<CommentIdAndReader> EnumerateDeveloperComments(string file)
        {
            Logger.LogInfo($"Loading developer comments from file: {file}");
            return from reader in
                       new Func<XmlReader>(() => XmlReader.Create(file))
                       .EmptyIfThrow()
                       .ProtectResource()
                   where reader.ReadToFollowing("members")
                   from apiReader in reader.Elements("member")
                   let commentId = apiReader.GetAttribute("name")
                   where commentId != null && commentId.Length > 2 && commentId[1] == ':'
                   select new CommentIdAndReader { CommentId = commentId, Reader = apiReader };
        }
        #endregion
    }
}