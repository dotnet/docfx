// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Common;

    public sealed class BuildMessageInfo
    {
        private Listener _listener;

        public IEnumerable<ILogItem> GetMessages(string file)
        {
            // todo : implement BuildMessageInfo.GetMessages
            return Enumerable.Empty<ILogItem>();
        }

        public ILoggerListener GetListener()
        {
            if (_listener == null)
            {
                _listener = new Listener(this);
            }
            return _listener;
        }

        public void Replay(string file)
        {
            foreach (var item in GetMessages(file))
            {
                Logger.Log(item);
            }
        }

        public static BuildMessageInfo Load(string file)
        {
            // todo : implement BuildMessageInfo.Load
            return new BuildMessageInfo();
        }

        public void Save(string file)
        {
            // todo : implement BuildMessageInfo.Save
        }

        private sealed class Listener : ILoggerListener
        {
            // todo : implement BuildMessageInfo.Listener.
            public Listener(BuildMessageInfo bmi)
            {
            }

            public void Dispose()
            {
            }

            public void Flush()
            {
            }

            public void WriteLine(ILogItem item)
            {
            }
        }
    }
}
