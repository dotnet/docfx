using System.Collections.Generic;
using System.Text;

namespace Mono.Documentation.Updater.Statistics
{
    public static class StatisticsFormatter
    {
        public static string Format(Dictionary<string, StatisticsStorage> input)
        {
            var result = new StringBuilder();
            foreach (var statisticsStoragePair in input)
            {
                var framework = statisticsStoragePair.Key;
                var statisticsStorage = statisticsStoragePair.Value;
                result.AppendLine($"Framework: {framework}");
                result.AppendLine("--------");
                foreach (var statistics in statisticsStorage.Values)
                {
                    var staticsItem = statistics.Key;
                    foreach (var statisticsValuePair in statistics.Value)
                    {
                        result.AppendLine($"{staticsItem} {statisticsValuePair.Key}: {statisticsValuePair.Value}");
                    }
                    result.AppendLine();
                }
            }

            return result.ToString();
        }
    }
}