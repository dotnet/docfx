// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
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
            _originFileName = CallContext.LogicalGetData(nameof(LoggerFileScope)) as string;
            if (_originFileName == null)
            {
                CallContext.LogicalSetData(nameof(LoggerFileScope), fileName);
            }
            else
            {
                CallContext.LogicalSetData(nameof(LoggerFileScope), fileName);
            }
        }

        public void Dispose()
        {
            CallContext.LogicalSetData(nameof(LoggerFileScope), _originFileName);
        }

        internal static string GetFileName()
        {
            return CallContext.LogicalGetData(nameof(LoggerFileScope)) as string;
        }
    }
}
