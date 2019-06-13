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
            var folder = GetFolder();

            lock (_locker)
            {
                _folderCollection.Add(folder);
            }

            Directory.CreateDirectory(folder);
            return folder;
        }

        protected string MoveToRandomFolder(string origin)
        {
            var folder = GetFolder();

            lock (_locker)
            {
                _folderCollection.Remove(folder);
                _folderCollection.Add(folder);
            }

            Directory.Move(origin, folder);
            return folder;
        }

        private string GetFolder()
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
            return folder;
        }


        #region IO related

        protected static string CreateFile(string fileName, string[] lines, string baseFolder)
        {
            if (lines == null)
            {
                throw new ArgumentNullException(nameof(lines));
            }
            var dir = Path.GetDirectoryName(fileName);
            dir = CreateDirectory(dir, baseFolder);
            var file = Path.Combine(baseFolder, fileName);
            File.WriteAllLines(file, lines);
            return file;
        }

        protected static string CreateFile(string fileName, string content, string baseFolder)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            if (baseFolder == null)
            {
                throw new ArgumentNullException(nameof(baseFolder));
            }
            var dir = Path.GetDirectoryName(fileName);
            dir = CreateDirectory(dir, baseFolder);
            var file = Path.Combine(baseFolder, fileName);
            File.WriteAllText(file, content);
            return file.Replace('\\', '/');
        }

        protected static string UpdateFile(string fileName, string[] lines, string baseFolder)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }
            if (lines == null)
            {
                throw new ArgumentNullException(nameof(lines));
            }
            if (baseFolder == null)
            {
                throw new ArgumentNullException(nameof(baseFolder));
            }
            File.Delete(Path.Combine(baseFolder, fileName));
            return CreateFile(fileName, lines, baseFolder);
        }

        protected static string UpdateFile(string fileName, string content, string baseFolder)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            if (baseFolder == null)
            {
                throw new ArgumentNullException(nameof(baseFolder));
            }
            File.Delete(Path.Combine(baseFolder, fileName));
            return CreateFile(fileName, content, baseFolder);
        }

        protected static string CreateDirectory(string dir, string baseFolder)
        {
            if (string.IsNullOrEmpty(dir))
            {
                return string.Empty;
            }
            if (baseFolder == null)
            {
                throw new ArgumentNullException(nameof(baseFolder));
            }
            var subDirectory = Path.Combine(baseFolder, dir);
            Directory.CreateDirectory(subDirectory);
            return subDirectory;
        }

        #endregion

        public virtual void Dispose()
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
