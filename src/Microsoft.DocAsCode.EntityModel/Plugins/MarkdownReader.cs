// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Utility;

    public class MarkdownReader
    {
        public static List<T> ReadMarkdownAsOverride<T>(string baseDir, string file) where T : IOverrideDocumentViewModel
        {
            return ReadMarkDownCore<T>(Path.Combine(baseDir, file)).ToList();
        }

        public static Dictionary<string, object> ReadMarkdownAsConceptual(string baseDir, string file)
        {
            var filePath = Path.Combine(baseDir, file);
            var repoInfo = GitUtility.GetGitDetail(filePath);
            return new Dictionary<string, object>
            {
                ["conceptual"] = File.ReadAllText(filePath),
                ["type"] = "Conceptual",
                ["source"] = new SourceDetail() { Remote = repoInfo },
                ["path"] = file,
            };
        }

        private static IEnumerable<T> ReadMarkDownCore<T>(string file) where T : IOverrideDocumentViewModel
        {
            var content = File.ReadAllText(file);
            var repoInfo = GitUtility.GetGitDetail(file);
            var lineIndex = GetLineIndex(content).ToList();
            var yamlDetails = YamlHeaderParser.Select(content);
            if (yamlDetails == null) yield break;
            var sections = from detail in yamlDetails
                           let id = detail.Id
                           from ms in detail.MatchedSections
                           from location in ms.Value.Locations
                           orderby location.StartLocation descending
                           select new { Detail = detail, Id = id, Location = location };
            var currentEnd = Coordinate.GetCoordinate(content);
            foreach (var item in sections)
            {
                if (!string.IsNullOrEmpty(item.Id))
                {
                    int start = lineIndex[item.Location.EndLocation.Line] + item.Location.EndLocation.Column + 1;
                    int end = lineIndex[currentEnd.Line] + currentEnd.Column + 1;
                    using (var sw = new StringWriter())
                    {
                        YamlUtility.Serialize(sw, item.Detail.Properties);
                        using (var sr = new StringReader(sw.ToString()))
                        {
                            var vm = YamlUtility.Deserialize<T>(sr);
                            vm.Conceptual = content.Substring(start, end - start + 1);
                            vm.Documentation = new SourceDetail { Remote = repoInfo, StartLine = item.Location.EndLocation.Line, EndLine = currentEnd.Line };
                            vm.Uid = item.Id;
                            yield return vm;
                        }
                    }
                }
                currentEnd = item.Location.StartLocation;
            }
        }

        private static IEnumerable<int> GetLineIndex(string content)
        {
            var index = 0;
            while (index >= 0)
            {
                yield return index;
                index = content.IndexOf('\n', index + 1);
            }
        }
    }
}
