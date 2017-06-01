// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public class LogCodesLogListener : ILoggerListener
    {
        public ConcurrentDictionary<string, HashSet<string>> Codes { get; } = new ConcurrentDictionary<string, HashSet<string>>();

        public void Dispose()
        {
        }

        public void Flush()
        {
        }

        public void WriteLine(ILogItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            if (string.IsNullOrEmpty(item.File) || string.IsNullOrEmpty(item.Code))
            {
                return;
            }
            Codes.AddOrUpdate(
                item.File,
                new HashSet<string>(new[] { item.Code }),
                (k, v) =>
                    {
                        v.Add(item.Code);
                        return v;
                    }
                );
        }
    }
}
