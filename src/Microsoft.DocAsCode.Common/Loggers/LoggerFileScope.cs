// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NetCore
namespace Microsoft.DocAsCode.Common
{
    using System;

    public sealed class LoggerFileScope : IDisposable
    {
        private readonly string _originFileName;

        public LoggerFileScope(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Phase name cannot be null or white space.", nameof(fileName));
            }
            _originFileName = GetFileName();
            SetFileName(fileName);
        }

        public void Dispose()
        {
            SetFileName(_originFileName);
        }

        internal static string GetFileName()
        {
            return LogicalCallContext.GetData(nameof(LoggerFileScope)) as string;
        }

        private static void SetFileName(string fileName)
        {
            LogicalCallContext.SetData(nameof(LoggerFileScope), fileName);
        }

        public static object Capture()
        {
            return new CapturedLoggerFileScope();
        }

        public static LoggerFileScope Restore(object captured)
        {
            var capturedScope = captured as CapturedLoggerFileScope;
            if (capturedScope == null)
            {
                return null;
            }
            return new LoggerFileScope(capturedScope.FileName);
        }

        private sealed class CapturedLoggerFileScope
        {
            public string FileName { get; } = GetFileName();
        }
    }
}
#endif