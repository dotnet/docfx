// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    internal sealed class MarkdownAggregateEngine : IMarkdownRewriteEngine
    {
        private readonly IMarkdownTokenAggregator _aggregator;
        private readonly Stack<IMarkdownToken> _parents = new Stack<IMarkdownToken>();

        public MarkdownAggregateEngine(IMarkdownEngine engine, IMarkdownTokenAggregator aggregator)
        {
            Engine = engine;
            _aggregator = aggregator;
            _parents.Push(null);
        }

        #region IMarkdownRewriteEngine Members

        public IMarkdownEngine Engine { get; }

        public ImmutableArray<IMarkdownToken> Rewrite(ImmutableArray<IMarkdownToken> tokens)
        {
            var context = new MarkdownTokenAggregateContext(_parents.Peek(), tokens);
            Aggregate(context);

            for (int i = 0; i < context.TokenLength; i++)
            {
                var token = context.GetToken(i);
                if (token is IMarkdownRewritable<IMarkdownToken> rewritable)
                {
                    _parents.Push(token);
                    var rewrittenToken = rewritable.Rewrite(this);
                    if (rewrittenToken != null &&
                        !object.ReferenceEquals(rewrittenToken, token))
                    {
                        context.SetToken(i, rewrittenToken);
                    }
                    _parents.Pop();
                    Aggregate(context);
                }
            }
            return context.ImmutableTokens;
        }

        private void Aggregate(MarkdownTokenAggregateContext aggContext)
        {
            while (aggContext.NextToken())
            {
                _aggregator.Aggregate(aggContext);
            }
        }

        ImmutableArray<IMarkdownToken> IMarkdownRewriteEngine.GetParents() =>
            throw new NotSupportedException();

        bool IMarkdownRewriteEngine.HasVariable(string name) =>
            throw new NotSupportedException();

        object IMarkdownRewriteEngine.GetVariable(string name) =>
            throw new NotSupportedException();

        void IMarkdownRewriteEngine.SetVariable(string name, object value) =>
            throw new NotSupportedException();

        void IMarkdownRewriteEngine.RemoveVariable(string name) =>
            throw new NotSupportedException();

        bool IMarkdownRewriteEngine.HasPostProcess(string name) =>
            throw new NotSupportedException();

        void IMarkdownRewriteEngine.SetPostProcess(string name, Action<IMarkdownRewriteEngine> action) =>
            throw new NotSupportedException();

        void IMarkdownRewriteEngine.RemovePostProcess(string name) =>
            throw new NotSupportedException();

        void IMarkdownRewriteEngine.Complete() { }

        void IMarkdownRewriteEngine.Initialize() { }

        #endregion
    }
}
