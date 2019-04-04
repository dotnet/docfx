// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals.Outputs
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;

    public class ExpandedDependencyMap
    {
        private HashSet<ExpandedDependencyItem> _dps;
        private OSPlatformSensitiveDictionary<HashSet<ExpandedDependencyItem>> _index = new OSPlatformSensitiveDictionary<HashSet<ExpandedDependencyItem>>();
        private OSPlatformSensitiveDictionary<HashSet<ExpandedDependencyItem>> _inverseIndex = new OSPlatformSensitiveDictionary<HashSet<ExpandedDependencyItem>>();

        private ExpandedDependencyMap(IEnumerable<ExpandedDependencyItem> dps)
        {
            _dps = new HashSet<ExpandedDependencyItem>(dps);
            BuildIndex();
        }

        public static ExpandedDependencyMap Empty { get; } = new ExpandedDependencyMap(Enumerable.Empty<ExpandedDependencyItem>());

        public void Save(TextWriter writer)
        {
            JsonUtility.Serialize(writer, _dps);
        }

        public static ExpandedDependencyMap Load(TextReader reader)
        {
            var dependencies = JsonUtility.Deserialize<IEnumerable<ExpandedDependencyItem>>(reader);
            return new ExpandedDependencyMap(dependencies);
        }

        public static ExpandedDependencyMap ConstructFromDependencyGraph(DependencyGraph dg)
        {
            var dps = from fn in dg.FromNodes
                      from d in dg.GetAllDependencyFrom(fn)
                      select ExpandedDependencyItem.ConvertFrom(d).ChangeFrom(fn);
            return new ExpandedDependencyMap(dps);
        }

        public IEnumerable<ExpandedDependencyItem> GetDependencyFrom(string from)
        {
            if (string.IsNullOrEmpty(from))
            {
                throw new ArgumentNullException(nameof(from));
            }
            if (_index.TryGetValue(from, out HashSet<ExpandedDependencyItem> items))
            {
                return items;
            }
            return Enumerable.Empty<ExpandedDependencyItem>();
        }

        public IEnumerable<ExpandedDependencyItem> GetDependencyTo(string to)
        {
            if (string.IsNullOrEmpty(to))
            {
                throw new ArgumentNullException(nameof(to));
            }
            if (_inverseIndex.TryGetValue(to, out HashSet<ExpandedDependencyItem> items))
            {
                return items;
            }
            return Enumerable.Empty<ExpandedDependencyItem>();
        }

        private void BuildIndex()
        {
            foreach (var dp in _dps)
            {
                if (!_index.TryGetValue(dp.From, out HashSet<ExpandedDependencyItem> items))
                {
                    _index[dp.From] = items = new HashSet<ExpandedDependencyItem>();
                }
                items.Add(dp);
                if (!_inverseIndex.TryGetValue(dp.To, out items))
                {
                    _inverseIndex[dp.To] = items = new HashSet<ExpandedDependencyItem>();
                }
                items.Add(dp);
            }
        }
    }
}
