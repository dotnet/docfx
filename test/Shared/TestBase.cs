// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Xunit;

    public class TestBase : IClassFixture<TestBase>, IDisposable
    {
        private readonly List<string> _folderCollection = new List<string>();
        private object _locker = new object();

        protected string GetRandomFolder()
        {
            var folder = Path.GetRandomFileName();
            if (Directory.Exists(folder))
            {
                folder = folder + DateTime.Now.ToString("HHmmssffff");
                if (Directory.Exists(folder))
                {
                    throw new InvalidOperationException($"Random folder name collides {folder}");
                }
            }

            lock (_locker)
            {
                _folderCollection.Add(folder);
            }

            Directory.CreateDirectory(folder);
            return folder;
        }

        public void Dispose()
        {
            try
            {
                foreach (var folder in _folderCollection)
                {
                    if (Directory.Exists(folder))
                    {
                        Directory.Delete(folder, true);
                    }
                }
            }
            catch
            {
            }
        }
    }
}
