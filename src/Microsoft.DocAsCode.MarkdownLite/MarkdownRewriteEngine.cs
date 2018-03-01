// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    public class MarkdownRewriteEngine : IMarkdownRewriteEngine
    {
        private readonly IMarkdownTokenRewriter _rewriter;
        private Stack<IMarkdownToken> _parents = new Stack<IMarkdownToken>();
        private Dictionary<string, object> _variables = new Dictionary<string, object>();
        private Dictionary<string, Action<IMarkdownRewriteEngine>> _postProcesses = new Dictionary<string, Action<IMarkdownRewriteEngine>>();

        public MarkdownRewriteEngine(IMarkdownEngine engine, IMarkdownTokenRewriter rewriter)
        {
            Engine = engine;
            _rewriter = rewriter;
        }

        #region IMarkdownRewriteEngine Members

        public IMarkdownEngine Engine { get; }

        public virtual ImmutableArray<IMarkdownToken> Rewrite(ImmutableArray<IMarkdownToken> tokens)
        {
            if (_rewriter == MarkdownTokenRewriterFactory.Null)
            {
                return tokens;
            }
            var result = tokens;
            for (int i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                var rewrittenToken = _rewriter.Rewrite(this, token);
                if (rewrittenToken != null &&
                    !object.ReferenceEquals(rewrittenToken, token))
                {
                    result = result.SetItem(i, rewrittenToken);
                    token = rewrittenToken;
                }
                if (token is IMarkdownRewritable<IMarkdownToken> rewritable)
                {
                    _parents.Push(token);
                    rewrittenToken = rewritable.Rewrite(this);
                    if (rewrittenToken != null &&
                        !object.ReferenceEquals(rewrittenToken, token))
                    {
                        result = result.SetItem(i, rewrittenToken);
                    }
                    _parents.Pop();
                }
            }
            return result;
        }

        public virtual ImmutableArray<IMarkdownToken> GetParents()
        {
            return ImmutableArray.CreateRange(_parents.Reverse());
        }

        public virtual bool HasVariable(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            return _variables.ContainsKey(name);
        }

        public virtual object GetVariable(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            _variables.TryGetValue(name, out object value);
            return value;
        }

        public virtual void SetVariable(string name, object value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            _variables[name] = value;
        }

        public virtual void RemoveVariable(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            _variables.Remove(name);
        }

        public virtual bool HasPostProcess(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            return _postProcesses.ContainsKey(name);
        }

        public virtual void SetPostProcess(string name, Action<IMarkdownRewriteEngine> action)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            _postProcesses[name] = action ?? throw new ArgumentNullException(nameof(action));
        }

        public virtual void RemovePostProcess(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            _postProcesses.Remove(name);
        }

        public virtual void Complete()
        {
            if (_postProcesses.Count > 0)
            {
                foreach (var item in _postProcesses.Values)
                {
                    item(this);
                }
            }
        }

        public void Initialize()
        {
            (_rewriter as IInitializable)?.Initialize(this);
        }

        #endregion
    }
}
