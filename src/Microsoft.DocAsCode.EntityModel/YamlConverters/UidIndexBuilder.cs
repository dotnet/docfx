namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class UidIndexBuilder
        : IPipelineItem<ConverterModel, IHasUidIndex, ConverterModel>
    {
        public ConverterModel Exec(ConverterModel arg, IHasUidIndex context)
        {
            BuildIndexCore(arg.Values, context);
            List<KeyValuePair<string, List<FileAndType>>> conflicts;
            if (!CheckUidIndex(context.UidIndex, out conflicts))
            {
                // todo : print conflicts
                throw new Exception("Conflict!");
            }
            return arg;
        }

        private void BuildIndexCore(IEnumerable<FileModel> models, IHasUidIndex context)
        {
            context.UidIndex = new Dictionary<string, HashSet<FileAndType>>();
            context.UidTree = new Dictionary<string, UidTreeNode>();
            foreach (var model in models)
            {
                foreach (var uid in model.GetUids())
                {
                    HashSet<FileAndType> set;
                    if (!context.UidIndex.TryGetValue(uid, out set))
                    {
                        set = new HashSet<FileAndType>();
                        context.UidIndex[uid] = set;
                    }
                    set.Add(model.FileAndType);
                }
                foreach (var r in model.GetRelationships())
                {
                    UidTreeNode node;
                    if (!context.UidTree.TryGetValue(r.Uid, out node))
                    {
                        node = new UidTreeNode(r.Uid, r.Parent);
                        context.UidTree[r.Uid] = node;
                    }
                    foreach (var child in r.Children)
                    {
                        node.Children.Add(child);
                    }
                    if (node.IsPage.HasValue && r.IsPage.HasValue)
                    {
                        if (node.IsPage != r.IsPage)
                        {
                            // todo : throw.
                        }
                    }
                    node.IsPage = node.IsPage ?? r.IsPage;
                }
            }
        }

        /// <summary>
        /// no same uid in override document and conceptual document.
        /// </summary>
        private static bool CheckUidIndex(Dictionary<string, HashSet<FileAndType>> index, out List<KeyValuePair<string, List<FileAndType>>> conflicts)
        {
            conflicts = new List<KeyValuePair<string, List<FileAndType>>>();
            var list = new List<FileAndType>();
            foreach (var item in index)
            {
                list.AddRange(from ft in item.Value
                              where ft.Type != DocumentType.ApiDocument
                              select ft);
                if (list.Count > 1)
                {
                    conflicts.Add(new KeyValuePair<string, List<FileAndType>>(item.Key, list));
                    list = new List<FileAndType>();
                }
                else
                {
                    list.Clear();
                }
            }
            return conflicts.Count == 0;
        }
    }
}
