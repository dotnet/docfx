namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class MarkdownReader
    {
        public static Dictionary<object, object> ReadMarkdownAsOverride(string baseDir, string file)
        {
            return new Dictionary<object, object>
            {
                ["items"] = ReadMarkDownCore(Path.Combine(baseDir, file)).ToList(),
            };
        }

        public static Dictionary<object, object> ReadMarkdownAsConceptual(string baseDir, string file)
        {
            return new Dictionary<object, object>
            {
                ["items"] = new[]
                {
                    new Dictionary<object, object>
                    {
                        ["uid"] = file,
                        ["conceptual"] = File.ReadAllText(Path.Combine(baseDir, file)),
                        ["type"] = "Conceptual",
                    }
                },
            };
        }

        private static IEnumerable<Dictionary<object, object>> ReadMarkDownCore(string file)
        {
            var content = File.ReadAllText(file);
            var lineIndex = GetLineIndex(content).ToList();
            var yamlDetails = YamlHeaderParser.Select(content);
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
                    int end = lineIndex[currentEnd.Line] + currentEnd.Column;
                    var dict = new Dictionary<object, object>();
                    foreach (var prop in item.Detail.Properties)
                    {
                        dict[prop.Key] = prop.Value;
                    }
                    dict["uid"] = item.Id;
                    dict["conceptual"] = content.Substring(start, end - start + 1);
                    yield return dict;
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
