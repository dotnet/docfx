using System;
using System.IO;

namespace Mono.Documentation.Updater.Statistics
{
    public static class StatisticsSaver
    {
        private const string DefaltStatisticsFileName = "statistics.txt";

        /// <summary>
        /// Save statistics to the file
        /// </summary>
        /// <param name="statisticsCollector">Statistics values which should be saved</param>
        /// <param name="statisticsFilePath">Path to the file where statistics should be saved</param>
        public static void Save(StatisticsCollector statisticsCollector, string statisticsFilePath)
        {
            if (Directory.Exists(statisticsFilePath))
                statisticsFilePath = Path.Combine(statisticsFilePath, DefaltStatisticsFileName);
            File.WriteAllText(statisticsFilePath, StatisticsFormatter.Format(statisticsCollector.Storages));
            Console.WriteLine($"Statistics saved to {statisticsFilePath}");
        }
    }
}