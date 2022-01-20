using System.Collections.Generic;

namespace ECMA2Yaml.Models
{
    public class TypeForwarding
    {
        public AssemblyInfo From { get; set; }
        public AssemblyInfo To { get; set; }
    }

    public class TypeForwardingChain
    {
        public Dictionary<string, List<TypeForwarding>> TypeForwardingsPerMoniker { get; set; }

        private List<VersionedValue<TypeForwarding>> _rawData;

        public TypeForwardingChain(List<VersionedValue<TypeForwarding>> fwds)
        {
            _rawData = fwds;
        }

        public void Build(HashSet<string> typeMonikers)
        {
            TypeForwardingsPerMoniker = new Dictionary<string, List<TypeForwarding>>();
            foreach (var fwd in _rawData)
            {
                foreach (var moniker in fwd.Monikers ?? typeMonikers)
                {
                    if (TypeForwardingsPerMoniker.TryGetValue(moniker, out var fwdList))
                    {
                        fwdList.Add(fwd.Value);
                    }
                    else
                    {
                        TypeForwardingsPerMoniker[moniker] = new List<TypeForwarding>() { fwd.Value };
                    }
                }
            }
        }
    }
}
