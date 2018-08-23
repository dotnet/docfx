// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    /// <summary>
    /// Represents a thread static context of the current document.
    /// </summary>
    /// <remarks>
    /// Due to markdig API design, it is not obvious to pass per document information and reusing the
    /// same markdown pipeline instance at the same time.
    /// Thus a thread static <see cref="InclusionContext"/> class is created to store per document information.
    /// </remarks>
    public static class InclusionContext
    {
        [ThreadStatic]
        private static object t_rootFile;

        [ThreadStatic]
        private static ImmutableStack<object> t_files;

        [ThreadStatic]
        private static ImmutableHashSet<object> t_dependencies;

        [ThreadStatic]
        public static Dictionary<string, object> Extensions;

        /// <summary>
        /// Gets the current file. This is the included file if the engine is currently parsing or rendering an include file.
        /// </summary>
        public static object File => t_files != null && !t_files.IsEmpty ? t_files.Peek() : null;

        /// <summary>
        /// Gets the root file, this is always the first file pushed to the context regardless of file inclusion.
        /// </summary>
        public static object RootFile => t_rootFile;

        /// <summary>
        /// Whether the content is included by other markdown files.
        /// </summary>
        public static bool IsInclude => t_files != null && t_files.Count() > 1;

        /// <summary>
        /// Gets all the dependencies referenced by the root markdown context.
        /// </summary>
        public static IEnumerable<object> Dependencies => (IEnumerable<object>)t_dependencies ?? ImmutableArray<object>.Empty;

        /// <summary>
        /// Creates a scope to use the specified file.
        /// </summary>
        public static IDisposable PushFile(object file)
        {
            var current = t_files ?? ImmutableStack<object>.Empty;

            if (current.IsEmpty)
            {
                // Clear dependencies for the root scope.
                t_dependencies = ImmutableHashSet<object>.Empty;
                t_rootFile = file;
            }
            else
            {
                t_dependencies = (t_dependencies ?? ImmutableHashSet<object>.Empty).Add(file);
            }

            t_files = current.Push(file);

            return new DelegatingDisposable(() =>
            {
                t_files = current;
                if (current.IsEmpty)
                {
                    t_rootFile = null;
                }
            });
        }

        /// <summary>
        /// Checks if the input file results in a circular reference.
        /// </summary>
        public static bool IsCircularReference(object file, out IEnumerable<object> dependencyChain)
        {
            dependencyChain = null;

            if (t_files.Contains(file))
            {
                dependencyChain = t_files.Reverse();
                return true;
            }

            return false;
        }

        class DelegatingDisposable : IDisposable
        {
            private readonly Action _dispose;

            public DelegatingDisposable(Action dispose) => _dispose = dispose;

            public void Dispose() => _dispose();
        }
    }
}
