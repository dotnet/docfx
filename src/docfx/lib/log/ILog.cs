// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal interface ILog
    {
        /// <summary>
        /// Reports a diagnostics message to the end user.
        /// </summary>
        void ReportDiagnostics(string code, string message, string file = null);
    }
}
