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
        private static ImmutableStack<object> t_files;

        [ThreadStatic]
        private static ImmutableHashSet<object> t_dependencies;

        /// <summary>
        /// Identifies the file that owns this content.
        /// </summary>
        public static object File => t_files != null && !t_files.IsEmpty ? t_files.Peek() : null;

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
            }
            else
            {
                t_dependencies = (t_dependencies ?? ImmutableHashSet<object>.Empty).Add(file);
            }

            t_files = current.Push(file);

            return new DelegatingDisposable(() => t_files = current);
        }

        /// <summary>
        /// Checks if the input file results in a circular reference.
        /// </summary>
        public static bool IsCircularReference(object file, out IEnumerable<object> dependencyChain)
        {
            dependencyChain = null;

            if (t_files.Contains(file))
            {
                dependencyChain = t_files.Concat(new[] { file });
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
