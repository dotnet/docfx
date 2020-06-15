// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class DisposableCollector : IDisposable
    {
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Add(IDisposable disposable)
        {
            lock (_disposables)
            {
                _disposables.Add(disposable);
            }
        }

        public void Dispose()
        {
            lock (_disposables)
            {
                foreach (var disposable in _disposables)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
