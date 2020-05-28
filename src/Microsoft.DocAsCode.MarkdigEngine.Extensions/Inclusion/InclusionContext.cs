// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

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
        private static readonly ThreadLocal<Stack<(object file, HashSet<object> dependencies, Stack<object> inclusionStack)>> t_markupStacks
                          = new ThreadLocal<Stack<(object file, HashSet<object> dependencies, Stack<object> inclusionStack)>>(
                                  () => new Stack<(object file, HashSet<object> dependencies, Stack<object> inclusionStack)>());

        /// <summary>
        /// Gets the current file. This is the included file if the engine is currently parsing or rendering an include file.
        /// </summary>
        public static object File
        {
            get
            {
                var markupStack = t_markupStacks.Value;
                return markupStack.Count > 0 ? markupStack.Peek().inclusionStack.Peek() : null;
            }
        }

        /// <summary>
        /// Gets the root file, this is always the first file pushed to the context regardless of file inclusion.
        /// </summary>
        public static object RootFile
        {
            get
            {
                var markupStack = t_markupStacks.Value;
                return markupStack.Count > 0 ? markupStack.Peek().file : null;
            }
        }

        /// <summary>
        /// Whether the content is included by other markdown files.
        /// </summary>
        public static bool IsInclude
        {
            get
            {
                var markupStack = t_markupStacks.Value;
                return markupStack.Count > 0 && markupStack.Peek().inclusionStack.Count > 1;
            }
        }

        /// <summary>
        /// Gets all the dependencies referenced by the root markdown context.
        /// </summary>
        public static IEnumerable<object> Dependencies
        {
            get
            {
                var markupStack = t_markupStacks.Value;
                return markupStack.Count > 0 ? (IEnumerable<object>)markupStack.Peek().dependencies : Array.Empty<object>();
            }
        }

        /// <summary>
        /// Creates a scope for calling <see cref="Markdig.Markdown.ToHtml(string, Markdig.MarkdownPipeline)"/>.
        /// </summary>
        public static IDisposable PushFile(object file)
        {
            var markupStack = t_markupStacks.Value;
            var inclusionStack = new Stack<object>();
            inclusionStack.Push(file);
            markupStack.Push((file, new HashSet<object>(), inclusionStack));

            return new DelegatingDisposable(() => markupStack.Pop());
        }

        /// <summary>
        /// Creates a scope for calling <see cref="Markdig.Markdown.ToHtml(string, Markdig.MarkdownPipeline)"/>
        /// when processing a markdown inclusion inside <see cref="HtmlInclusionBlockRenderer"/> and <see cref="HtmlInclusionInlineRenderer"/>.
        /// </summary>
        public static IDisposable PushInclusion(object file)
        {
            var inclusionStack = t_markupStacks.Value.Peek().inclusionStack;
            inclusionStack.Push(file);

            return new DelegatingDisposable(() => inclusionStack.Pop());
        }

        /// <summary>
        /// Push dependency
        /// </summary>
        public static void PushDependency(object file)
        {
            t_markupStacks.Value.Peek().dependencies.Add(file);
        }

        /// <summary>
        /// Checks if the input file results in a circular reference.
        /// </summary>
        public static bool IsCircularReference(object file, out IEnumerable<object> dependencyChain)
        {
            dependencyChain = null;

            var markupStack = t_markupStacks.Value;
            var inclusionStack = markupStack.Count > 0 ? markupStack.Peek().inclusionStack : null;
            if (inclusionStack != null && inclusionStack.Contains(file))
            {
                dependencyChain = inclusionStack.Reverse();
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
