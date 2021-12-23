using System.Collections.Generic;

namespace Mono.Documentation.Updater.Statistics
{
    /// <summary>
    /// The class stores statistics for different frameworks
    /// </summary>
    public class StatisticsCollector
    {
        /// <summary>
        /// Collected statistics for each framework
        /// </summary>
        public Dictionary<string, StatisticsStorage> Storages { get; } =
            new Dictionary<string, StatisticsStorage>();

        /// <summary>
        /// Change metric value for the item
        /// </summary>
        /// <param name="framework">The framework name which statistics is being collected</param>
        /// <param name="item">The item which metrics value is changing</param>
        /// <param name="metrics">The metrics which value is changing</param>
        /// <param name="delta">The value by which the metrics value should be changed</param>
        public void AddMetric(string framework, StatisticsItem item, StatisticsMetrics metrics, int delta = 1)
        {
            if (!Storages.ContainsKey(framework))
            {
                Storages[framework] = new StatisticsStorage();
            }

            Storages[framework].AddMetric(item, metrics, delta);
        }
    }
}