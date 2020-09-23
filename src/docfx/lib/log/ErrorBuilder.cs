// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Microsoft.Docs.Build
{
    internal abstract class ErrorBuilder
    {
        public static readonly ErrorBuilder Null = new NullErrorBuilder();

        public abstract bool HasError { get; }

        public abstract void Add(Error error);

        public abstract bool FileHasError(FilePath file);

        public void AddIfNotNull(Error? error)
        {
            if (error != null)
            {
                Add(error);
            }
        }

        public void AddRange(IEnumerable<Error> errors)
        {
            foreach (var error in errors)
            {
                Add(error);
            }
        }

        public void AddRange(IEnumerable<DocfxException> exceptions)
        {
            foreach (var exception in exceptions)
            {
                Log.Write(exception);
                Add(exception.Error);
            }
        }

        public ErrorBuilder With(Func<Error, Error> convert)
        {
            return new DelegatingErrorBuilder(this, convert);
        }

        public ErrorBuilder WithDocsetPath(string workingDirectory, string docsetPath)
        {
            var docsetBasePath = new PathString(Path.GetRelativePath(workingDirectory, docsetPath));

            return With(error =>
            {
                // Convert from path relative to docset to path relative to working directory
                if (!docsetBasePath.IsDefault)
                {
                    if (error.Source != null)
                    {
                        error = error.WithSource(error.Source.WithFile(error.Source.File.WithPath(docsetBasePath.Concat(error.Source.File.Path))));
                    }

                    if (error.OriginalPath != null)
                    {
                        error = error.WithOriginalPath(docsetBasePath.Concat(error.OriginalPath.Value));
                    }
                }
                return error;
            });
        }

        public ErrorBuilder WithCustormRule(JsonSchema schema)
        {
            return With(error =>
            {
                if (!string.IsNullOrEmpty(error.PropertyPath) &&
                    schema.Rules.TryGetValue(error.PropertyPath, out var attributeCustomRules) &&
                    attributeCustomRules.TryGetValue(error.Code, out var customRule))
                {
                    return error.WithCustomRule(customRule);
                }
                return error;
            });
        }

        private class NullErrorBuilder : ErrorBuilder
        {
            public override bool HasError => throw new NotSupportedException();

            public override void Add(Error error) { }

            public override bool FileHasError(FilePath file) => throw new NotSupportedException();
        }

        private class DelegatingErrorBuilder : ErrorBuilder
        {
            private readonly ErrorBuilder _errors;
            private readonly Func<Error, Error> _convert;

            private int _errorCount;

            public override bool HasError => Volatile.Read(ref _errorCount) > 0;

            public override bool FileHasError(FilePath file) => throw new NotSupportedException();

            public DelegatingErrorBuilder(ErrorBuilder errors, Func<Error, Error> convert)
            {
                _errors = errors;
                _convert = convert;
            }

            public override void Add(Error error)
            {
                error = _convert(error);

                if (error.Level == ErrorLevel.Error)
                {
                    Interlocked.Increment(ref _errorCount);
                }

                _errors.Add(error);
            }
        }
    }
}
