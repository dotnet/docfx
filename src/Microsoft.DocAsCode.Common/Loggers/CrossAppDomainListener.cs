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
