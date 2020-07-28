// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;

namespace Microsoft.Docs.Build
{
    internal class DocsetErrorWriter : ErrorBuilder
    {
        private readonly ErrorBuilder _errors;
        private readonly PathString _docsetBasePath;

        private int _errorCount;

        public override bool HasError => Volatile.Read(ref _errorCount) > 0;

        public override bool FileHasError(FilePath file) => throw new NotSupportedException();

        public DocsetErrorWriter(ErrorBuilder errors, string workingDirectory, string docsetPath)
        {
            _errors = errors;
            _docsetBasePath = new PathString(Path.GetRelativePath(workingDirectory, docsetPath));
        }

        public override void Add(Error error)
        {
            // Convert from path relative to docset to path relative to working directory
            if (!_docsetBasePath.IsDefault)
            {
                if (error.Source != null)
                {
                    error = error.WithSource(error.Source.WithFile(error.Source.File.WithPath(_docsetBasePath.Concat(error.Source.File.Path))));
                }

                if (error.OriginalPath != null)
                {
                    error = error.WithOriginalPath(_docsetBasePath.Concat(error.OriginalPath.Value));
                }
            }

            if (error.Level == ErrorLevel.Error)
            {
                Interlocked.Increment(ref _errorCount);
            }

            _errors.Add(error);
        }
    }
}
