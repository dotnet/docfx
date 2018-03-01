// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.IO;

    public abstract class FileWriterBase : IFileWriter
    {
        private const int MaxRetry = 3;

        public FileWriterBase(string outputFolder)
        {
            ExpandedOutputFolder = Path.GetFullPath(Environment.ExpandEnvironmentVariables(outputFolder));
            EnsureFolder(ExpandedOutputFolder);
            OutputFolder = outputFolder;
        }

        public string OutputFolder { get; }

        public string ExpandedOutputFolder { get; }

        #region IFileWriter

        public abstract void Copy(PathMapping sourceFileName, RelativePath destFileName);

        public abstract Stream Create(RelativePath filePath);

        public abstract IFileReader CreateReader();

        #endregion

        #region Help Methods

        protected internal static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        protected string GetRandomEntry()
        {
            string name;
            string path;
            do
            {
                name = Path.GetRandomFileName();
                path = Path.Combine(ExpandedOutputFolder, name);
            } while (Directory.Exists(path) || File.Exists(path));
            return name;
        }

        protected Tuple<string, FileStream> CreateRandomFileStream()
        {
            return RetryIO(() =>
            {
                var file = GetRandomEntry();
                return Tuple.Create(file, File.Create(Path.Combine(ExpandedOutputFolder, file)));
            });
        }

        protected static T RetryIO<T>(Func<T> func)
        {
            var count = 0;
            while (true)
            {
                try
                {
                    return func();
                }
                catch (IOException)
                {
                    if (count++ >= MaxRetry)
                    {
                        throw;
                    }
                }
            }
        }

        protected static void RetryIO(Action action)
        {
            var count = 0;
            while (true)
            {
                try
                {
                    action();
                    return;
                }
                catch (IOException)
                {
                    if (count++ >= MaxRetry)
                    {
                        throw;
                    }
                }
            }
        }

        #endregion
    }
}
