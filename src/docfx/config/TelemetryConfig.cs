// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal sealed class TelemetryConfig
    {
        /// <summary>
        /// Gets the correlation id
        /// </summary>
        public readonly string CorrelationId;

        /// <summary>
        /// Gets the dimensions for event telemetry
        /// </summary>
        public readonly Dictionary<string, string> EventDimensions = new Dictionary<string, string>();
    }
}
