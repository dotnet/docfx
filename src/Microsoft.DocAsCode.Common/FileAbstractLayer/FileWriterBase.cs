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
            OutputFolder = outputFolder;
        }

        public string OutputFolder { get; }

        #region IFileWriter

        public abstract void Copy(PathMapping sourceFileName, RelativePath destFileName);

        public abstract FileStream Create(RelativePath filePath);

        public abstract IFileReader CreateReader();

        #endregion

        #region Help Methods

        protected string GetRandomEntry()
        {
            string name;
            do
            {
                name = Path.GetRandomFileName();
            } while (Directory.Exists(Path.Combine(OutputFolder, name)) || File.Exists(Path.Combine(OutputFolder, name)));
            return name;
        }

        protected Tuple<string, FileStream> CreateRandomFileStream()
        {
            return RetryIO(() =>
            {
                var file = GetRandomEntry();
                return Tuple.Create(file, File.Create(Path.Combine(OutputFolder, file)));
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
