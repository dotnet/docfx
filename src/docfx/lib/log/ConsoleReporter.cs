// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal class ConsoleReporter : IReporter
    {
        public void Error(string code, string message, string file = "", int line = 0, int column = 0)
        {
            Report(new ReportItem(ReportLevel.Error, code, message, file, line, column));
        }

        public void Warn(string code, string message, string file = "", int line = 0, int column = 0)
        {
            Report(new ReportItem(ReportLevel.Warning, code, message, file, line, column));
        }

        public void Info(string code, string message, string file = "", int line = 0, int column = 0)
        {
            Report(new ReportItem(ReportLevel.Info, code, message, file, line, column));
        }

        public void Report(ReportItem item)
        {
        }
    }
}
