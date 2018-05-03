// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal interface IReporter
    {
        void Error(Code code, string message, string file = "", int line = 0, int column = 0);

        void Warn(Code code, string message, string file = "", int line = 0, int column = 0);

        void Info(Code code, string message, string file = "", int line = 0, int column = 0);

        void Report(ReportItem item);
    }
}
