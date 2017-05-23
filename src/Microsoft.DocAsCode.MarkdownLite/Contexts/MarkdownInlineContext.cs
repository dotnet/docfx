// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class MarkdownInlineContext : IMarkdownContext
    {
        public const string IsInLink = "IsInLink";
        private static readonly object BoxedFalse = false;

        public MarkdownInlineContext(ImmutableList<IMarkdownRule> rules)
            : this(rules, ImmutableDictionary<string, object>.Empty.Add(IsInLink, BoxedFalse))
        {
        }

        protected MarkdownInlineContext(ImmutableList<IMarkdownRule> rules, ImmutableDictionary<string, object> variables)
        {
            Rules = rules;
            Variables = variables;
        }

        public ImmutableList<IMarkdownRule> Rules { get; }

        public ImmutableDictionary<string, object> Variables { get; private set; }

        public virtual IMarkdownContext CreateContext(ImmutableDictionary<string, object> variables)
        {
            var clone = (MarkdownInlineContext)MemberwiseClone();
            clone.Variables = variables;
            return clone;
        }

        public static bool GetIsInLink(IMarkdownContext context)
        {
            if (!context.Variables.TryGetValue(IsInLink, out object value))
            {
                return false;
            }
            return value as bool? ?? false;
        }
    }
}
