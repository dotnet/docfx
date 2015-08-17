namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System.Linq;
    using System.Threading.Tasks;

    public class OverrideDocumentHandler : IPipelineItem<ConverterModel, IHasUidIndex, ConverterModel>
    {
        public ConverterModel Exec(ConverterModel arg, IHasUidIndex context)
        {
            foreach (var pair in context.UidIndex)
            {
                var list = (from ft in pair.Value
                            select new { ft, m = arg[ft].GetItem(pair.Key) }).ToList();
                var odIndex = list.FindIndex(x => x.ft.Type == DocumentType.OverrideDocument);
                if (odIndex != -1)
                {
                    var od = list[odIndex];
                    list.RemoveAt(odIndex);
                    foreach (var item in list)
                    {
                        foreach (var p in od.m)
                        {
                            item.m[p.Key] = p.Value;
                        }
                    }
                }
            }
            return arg;
        }
    }
}
