using System;
using System.Collections.Generic;

namespace ECMA2Yaml.Models
{
    public class VersionedProperty<VT> where VT : IEquatable<VT>
    {
        public Dictionary<string, List<VT>> ValuesPerMoniker { get; private set; }
        public Dictionary<VT, List<string>> MonikersPerValue { get; private set; }

        public VersionedProperty(Dictionary<string, List<VT>> valuesPerMoniker)
        {
            ValuesPerMoniker = valuesPerMoniker;

            MonikersPerValue = new Dictionary<VT, List<string>>();
            foreach (var pair in valuesPerMoniker)
            {
                foreach (var value in pair.Value)
                {
                    if (MonikersPerValue.TryGetValue(value, out List<string> monikerList))
                    {
                        monikerList.Add(pair.Key);
                    }
                    else
                    {
                        MonikersPerValue[value] = new List<string>() { pair.Key };
                    }
                }
            }
        }
    }
}
