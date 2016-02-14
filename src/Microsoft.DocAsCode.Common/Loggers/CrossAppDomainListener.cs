// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NetCore
namespace Microsoft.DocAsCode.Common
{
    using System;

    public class CrossAppDomainListener : MarshalByRefObject, ILoggerListener
    {
        public LogLevel LogLevelThreshold
        {
            get
            {
                return Logger.LogLevelThreshold;
            }

            set
            {
                Logger.LogLevelThreshold = value;
            }
        }

        public void Dispose()
        {
        }

        public void WriteLine(ILogItem item)
        {
            Logger.Log(item);
        }

        public void Flush()
        {
            Logger.Flush();
        }
    }
}
#endif