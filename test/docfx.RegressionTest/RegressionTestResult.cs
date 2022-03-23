// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class RegressionTestResult
{
    public bool Succeeded { get; set; }

    public TimeSpan BuildTime { get; set; }

    public long PeakMemory { get; set; }

    public int? Timeout { get; set; }

    public string? Diff { get; set; }

    public string? HotMethods { get; set; }

    public string? CrashMessage { get; set; }

    public int MoreLines { get; set; }

    public override string ToString() => $"{{{nameof(Succeeded)}={Succeeded}, " +
            $"{nameof(BuildTime)}={BuildTime.TotalSeconds}s, " +
            $"{nameof(Timeout)}={Timeout}s, " +
            $"{nameof(PeakMemory)}={PeakMemory}, " +
            $"{nameof(Diff)}={(Diff?.Length > 0 ? "Yes" : "No")}, " +
            $"{nameof(MoreLines)}={MoreLines}}}, " +
            $"{nameof(CrashMessage)}={CrashMessage}";
}
