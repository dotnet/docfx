// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class LogItem
    {
        public LogLevel Level { get; }

        public string Message { get; }

        public DateTime LogTimeUtc { get; }

        public string CorrelationId { get; }

        public IReadOnlyDictionary<string, object> DataBag { get; }

        public LogItem(LogLevel level, string message, string correlationId, IReadOnlyDictionary<string, object> bag)
        {
            Level = level;
            Message = message;
            LogTimeUtc = DateTime.UtcNow;
            CorrelationId = correlationId;
            DataBag = bag;
        }
    }
}
