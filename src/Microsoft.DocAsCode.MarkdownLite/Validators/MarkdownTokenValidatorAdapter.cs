namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    internal sealed class MarkdownTokenValidatorAdapter : IMarkdownTokenRewriter
    {
        public ImmutableArray<IMarkdownTokenValidator> Validators { get; }

        public MarkdownTokenValidatorAdapter(IEnumerable<IMarkdownTokenValidator> validators)
        {
            Validators = validators.ToImmutableArray();
        }

        public IMarkdownToken Rewrite(IMarkdownRewriteEngine engine, IMarkdownToken token)
        {
            foreach (var validator in Validators)
            {
                validator.Validate(token);
            }
            return null;
        }
    }
}
