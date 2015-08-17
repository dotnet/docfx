namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public class MergeApiYamlHandler : IPipelineItem<ConverterModel, IHasUidIndex, ConverterModel>
    {
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
                var properties = new Dictionary<string, object>();
                foreach (var apiDoc in apiDocs)
                {
                    var relationShip = context.UidTree[pair.Key];
                    Debug.Assert(relationShip != null);
                    if (parent == null)
                    {
                        parent = relationShip.Parent;
                        properties["parent"] = parent;
                    }
                    else if (parent != relationShip.Parent)
                    {
                        // todo : log
                        Console.WriteLine($@"Different parent for same uid:
    uid: {pair.Key}
    parent: {parent} in {apiDocs[0].File}
    parent: {relationShip.Parent} in {apiDoc.File}");
                        throw new Exception();
                    }
                    children.UnionWith(relationShip.Children);
                    foreach (var p in models[apiDoc].GetItem(pair.Key))
                    {
                        if (!properties.ContainsKey(p.Key))
                        {
                            if (p.Key != "parent" && p.Key != "children")
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
            // todo : resolve link, how??
            return value;
        }
    }
}
