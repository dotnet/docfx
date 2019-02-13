// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using Microsoft.ApplicationInsights;

namespace Microsoft.Docs.Build
{
    internal static class Telemetry
    {
        private static readonly TelemetryClient s_telemetryClient;

        static Telemetry()
        {
            s_telemetryClient = new TelemetryClient();
        }

        public static void TrackValue(string metric, int value)
        {
            
        }

        public static void TrackException(Exception ex)
        {

        }

        public static void Flush()
        {
            s_telemetryClient.Flush();
        }
    }
}
