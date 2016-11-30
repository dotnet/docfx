// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    public class ProcessDetail
    {
        public string ExecutorPath { get; set; }
        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
        public string StandardOutput { get; set; }
        public string StandardError { get; set; }
        public int ExitCode { get; set; }
        public int ProcessId { get; set; }
    }
}
