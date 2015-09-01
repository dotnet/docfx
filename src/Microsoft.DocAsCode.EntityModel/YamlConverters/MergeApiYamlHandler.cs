// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public class MergeApiYamlHandler : IPipelineItem<ConverterModel, IHasUidIndex, ConverterModel>
    {
        // todo : move to context, or constructor?.
        public static readonly RelativePath OutputFolder = (RelativePath)"api/";

        public ConverterModel Exec(ConverterModel arg, IHasUidIndex context)
        {
            var result = new ConverterModel(arg.BaseDir);
            foreach (var item in MergeApiDocument(arg, context))
            {
                // todo : get file name from uid
                result.Add(new FileAndType(arg.BaseDir, "get file name from uid?", DocumentType.ApiDocument), item);
            }
            return result;
        }

        private static IEnumerable<FileModel> MergeApiDocument(ConverterModel models, IHasUidIndex context)
        {
            foreach (var pair in context.UidIndex)
            {
                var apiDocs = pair.Value.Where(ft => ft.Type == DocumentType.ApiDocument).ToList();
                if (apiDocs.Count <= 1)
                {
                    continue;
                }
                string parent = null;
                var children = new HashSet<string>();
                var properties = new Dictionary<object, object>();
                foreach (var apiDoc in apiDocs)
                {
                    var relationship = context.UidTree[pair.Key];
                    Debug.Assert(relationship != null);
                    if (parent == null)
                    {
                        parent = relationship.Parent;
                        properties["parent"] = parent;
                    }
                    else if (parent != relationship.Parent)
                    {
                        // todo : log
                        Console.WriteLine($@"Different parent for same uid:
    uid: {pair.Key}
    parent: {parent} in {apiDocs[0].File}
    parent: {relationship.Parent} in {apiDoc.File}");
                        throw new Exception();
                    }
                    children.UnionWith(relationship.Children);
                    foreach (var p in models[apiDoc].GetItem(pair.Key))
                    {
                        if (!properties.ContainsKey(p.Key))
                        {
                            if (!"parent".Equals(p.Key) && !"children".Equals(p.Key))
                            {
                                properties[p.Key] = ResolveLink(apiDoc, p.Value);
                            }
                        }
                    }
                }
                properties["children"] = children;
                foreach (var apiDoc in apiDocs)
                {
                    models[apiDoc].Replace(pair.Key, properties);
                }
            }
            return new FileModel[0];
        }

        private static object ResolveLink(FileAndType ft, object value)
        {
            return RelativePathRewriter.Rewrite(value, (RelativePath)ft.File, OutputFolder);
        }
    }
}
