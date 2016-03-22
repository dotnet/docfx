// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Utility;

    public class MarkdownReader
    {
        public static List<OverwriteDocumentModel> ReadMarkdownAsOverwrite(string baseDir, string file)
        {
            // Order the list from top to bottom
            var list = ReadMarkDownCore(Path.Combine(baseDir, file)).ToList();
            list.Reverse();
            return list;
        }

        public static Dictionary<string, object> ReadMarkdownAsConceptual(string baseDir, string file)
        {
            var filePath = Path.Combine(baseDir, file);
            var repoInfo = GitUtility.GetGitDetail(filePath);
            return new Dictionary<string, object>
            {
                [Constants.PropertyName.Conceptual] = File.ReadAllText(filePath),
                [Constants.PropertyName.Type] = "Conceptual",
                [Constants.PropertyName.Source] = new SourceDetail() { Remote = repoInfo },
                [Constants.PropertyName.Path] = file,
            };
        }

        private static IEnumerable<OverwriteDocumentModel> ReadMarkDownCore(string file)
        {
            var content = File.ReadAllText(file);
            var repoInfo = GitUtility.GetGitDetail(file);
            var lineIndex = GetLineIndex(content).ToList();
            var yamlDetails = YamlHeaderParser.Select(content);
            var sections = from detail in yamlDetails
                           let id = detail.Id
                           from location in detail.MatchedSection.Locations
                           orderby location.StartLocation descending
                           select new { Detail = detail, Id = id, Location = location };
            var currentEnd = Coordinate.GetCoordinate(content);
            foreach (var item in sections)
            {
                if (!string.IsNullOrEmpty(item.Id))
                {
                    int start = lineIndex[item.Location.EndLocation.Line] + item.Location.EndLocation.Column + 1;
                    int end = lineIndex[currentEnd.Line] + currentEnd.Column + 1;
                    yield return new OverwriteDocumentModel
                    {
                        Uid = item.Id,
                        Metadata = item.Detail.Properties,
                        Conceptual = content.Substring(start, end - start + 1),
                        Documentation = new SourceDetail
                        {
                            Remote = repoInfo,
                            StartLine = item.Location.EndLocation.Line,
                            EndLine = currentEnd.Line,
                            Path = Path.GetFullPath(file).ToDisplayPath()
                        }
                    };
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
