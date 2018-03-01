// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    public static class MarkdownTokenTreeValidatorFactory
    {
        public static readonly IMarkdownTokenTreeValidator Null = new NullTokenTreeValidator();

        public static IMarkdownTokenTreeValidator Combine(
            params IMarkdownTokenTreeValidator[] validators)
        {
            return Combine((IEnumerable<IMarkdownTokenTreeValidator>)validators);
        }

        public static IMarkdownTokenTreeValidator Combine(
            IEnumerable<IMarkdownTokenTreeValidator> validators)
        {
            if (validators == null)
            {
                return Null;
            }
            var array = (from v in validators
                         where v != null && v != Null
                         select v).ToArray();
            if (array.Length == 0)
            {
                return Null;
            }
            return new CompositeTokenTreeValidator(array);
        }

        private sealed class NullTokenTreeValidator : IMarkdownTokenTreeValidator
        {
            public void Validate(ImmutableArray<IMarkdownToken> tokens)
            {
            }
        }

        private sealed class CompositeTokenTreeValidator
            : IMarkdownTokenTreeValidator
        {
            private IMarkdownTokenTreeValidator[] _validators;

            public CompositeTokenTreeValidator(IMarkdownTokenTreeValidator[] validators)
            {
                _validators = validators;
            }

            public void Validate(ImmutableArray<IMarkdownToken> tokens)
            {
                foreach (var v in _validators)
                {
                    v.Validate(tokens);
                }
            }
        }
    }
}
