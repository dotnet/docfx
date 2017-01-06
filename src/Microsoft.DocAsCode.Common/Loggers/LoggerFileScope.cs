// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NetCore
namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Runtime.Remoting.Messaging;

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
            return CallContext.LogicalGetData(nameof(LoggerFileScope)) as string;
        }

        private static void SetFileName(string fileName)
        {
            CallContext.LogicalSetData(nameof(LoggerFileScope), fileName);
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

        public void Execute(Action action)
        {
            try
            {
                action();
            }
            catch (Exception)
            {
                Logger.LogError("Error occurred.");
                throw;
            }
        }

        public TResult Execute<TResult>(Func<TResult> func)
        {
            try
            {
                return func();
            }
            catch (Exception)
            {
                Logger.LogError("Error occurred.");
                throw;
            }
        }

        public static void Execute(string fileName, Action action)
        {
            using (var scope = new LoggerFileScope(fileName))
            {
                scope.Execute(action);
            }
        }

        public static TResult Execute<TResult>(string fileName, Func<TResult> func)
        {
            using (var scope = new LoggerFileScope(fileName))
            {
                return scope.Execute(func);
            }
        }

        private sealed class CapturedLoggerFileScope
        {
            public string FileName { get; } = GetFileName();
        }
    }
}
#endif