// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;

    public static class MarkdownTokenValidatorFactory
    {
        public static IMarkdownTokenValidator FromLambda<TToken>(
            Action<TToken> validator)
            where TToken : class, IMarkdownToken
            => FromLambda(validator, null);

        public static IMarkdownTokenValidator FromLambda<TToken>(
            Action<TToken> validator,
            Action<IMarkdownRewriteEngine> initializer)
            where TToken : class, IMarkdownToken
        {
            if (validator == null)
            {
                throw new ArgumentNullException(nameof(validator));
            }
            return new MarkdownLambdaTokenValidator<TToken>(validator, initializer);
        }

        private sealed class MarkdownLambdaTokenValidator<TToken>
            : IMarkdownTokenValidator, IInitializable
            where TToken : class, IMarkdownToken
        {
            public Action<TToken> Validator { get; }

            public Action<IMarkdownRewriteEngine> Initializer { get; }

            public MarkdownLambdaTokenValidator(Action<TToken> validator, Action<IMarkdownRewriteEngine> initializer)
            {
                Validator = validator;
                Initializer = initializer;
            }

            public void Validate(IMarkdownToken token)
            {
                var t = token as TToken;
                if (t != null)
                {
                    Validator(t);
                }
            }

            public void Initialize(IMarkdownRewriteEngine rewriteEngine)
            {
                Initializer?.Invoke(rewriteEngine);
            }
        }
    }
}
