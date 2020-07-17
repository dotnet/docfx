using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripleCrownValidation.DependencyMap
{
    public static class DependencyMapHelper
    {
        public static List<DependencyItem> LoadDependentFileInfo(string dependencyMapFile)
        {
            var dependencyItems = new List<DependencyItem>();
            using (var stream = File.OpenRead(dependencyMapFile))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var dependentInfo = !string.IsNullOrEmpty(line) ? JsonConvert.DeserializeObject<DependencyItem>(line) : null;

                    if (dependentInfo != null && !string.IsNullOrEmpty(dependentInfo.FromFilePath) && !string.IsNullOrEmpty(dependentInfo.ToFilePath))
                    {
                        var normalizedToFilePath = dependentInfo.ToFilePath.BackSlashToForwardSlash();
                        var normalizedFromFilePath = dependentInfo.FromFilePath.BackSlashToForwardSlash();

                        dependencyItems.Add(
                            new DependencyItem
                            {
                                FromFilePath = normalizedFromFilePath,
                                ToFilePath = normalizedToFilePath,
                                DependencyType = dependentInfo.DependencyType,
                                Version = dependentInfo.Version
                            });
                    }

                }
            }

            return dependencyItems;
        }
    }
}
