// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.ApplicationInsights;

namespace Microsoft.Docs.Build
{
    internal static class Telemetry
    {
        private static readonly TelemetryClient s_telemetryClient = CreateTelemetryClient();

        public static void TrackValue(string metric, int value)
        {
            s_telemetryClient.GetMetric(metric, "d1", "d2", "d3").TrackValue(value, "v1", "v2", "v3");
        }

        public static void TrackException(Exception ex)
        {
            s_telemetryClient.TrackException(ex);
        }

        public static void Flush()
        {
            // Default timeout of 100 sec is used
            s_telemetryClient.Flush();
        }

        private static TelemetryClient CreateTelemetryClient()
        {
            var telemetryClient = new TelemetryClient();

            telemetryClient.Context.Component.Version = typeof(Telemetry).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            telemetryClient.Context.Device.OperatingSystem = RuntimeInformation.OSDescription;

            return telemetryClient;
        }
    }
}
