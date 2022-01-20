using System;
using System.Collections.Generic;

namespace Mono.Documentation.Updater.Statistics
{
    /// <summary>
    /// The class stores statistics: int value for each StatisticsItem-StatisticsMetrics pair
    /// </summary>
    public class StatisticsStorage
    {
        public Dictionary<StatisticsItem, Dictionary<StatisticsMetrics, int>> Values { get; } = new Dictionary<StatisticsItem, Dictionary<StatisticsMetrics, int>>();

        public StatisticsStorage()
        {
            foreach (var statisticsItem in Enum.GetValues(typeof(StatisticsItem)))
            {
                var metrics = Values[(StatisticsItem)statisticsItem] = new Dictionary<StatisticsMetrics, int>();
                foreach (var statisticsMetrics in Enum.GetValues(typeof(StatisticsMetrics)))
                {
                    metrics[(StatisticsMetrics) statisticsMetrics] = 0;
                }
            }
        }

        public void AddMetric(StatisticsItem item, StatisticsMetrics metrics, int delta = 1)
        {
            Values[item][metrics] += delta;
        }
    }
}