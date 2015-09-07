// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System.Linq;

    public class OverrideDocumentHandler : IPipelineItem<ConverterModel, IHasUidIndex, ConverterModel>
    {
        public ConverterModel Exec(ConverterModel arg, IHasUidIndex context)
        {
            //foreach (var pair in context.UidIndex)
            //{
            //    var list = (from ft in pair.Value
            //                select new { ft, m = arg[ft].GetItem(pair.Key) }).ToList();
            //    var odIndex = list.FindIndex(x => x.ft.Type == DocumentType.Override);
            //    if (odIndex != -1)
            //    {
            //        var od = list[odIndex];
            //        var rp = (RelativePath)od.ft.File;
            //        list.RemoveAt(odIndex);
            //        foreach (var item in list)
            //        {
            //            foreach (var p in od.m)
            //            {
            //                item.m[p.Key] = RelativePathRewriter.Rewrite(p.Value, rp, (RelativePath)item.ft.File);
            //            }
            //        }
            //    }
            //}
            return arg;
        }
    }
}
