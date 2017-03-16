using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dfm_test
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;

    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;


    [Export("dfm", typeof(IMarkdownServiceProvider))]
    public class DfmServiceProvider : IMarkdownServiceProvider
    {
        public IMarkdownService CreateMarkdownService(MarkdownServiceParameters parameters)
        {
            return new DfmService(
                parameters.BasePath,
                parameters.Tokens,
                MarkdownTokenTreeValidatorFactory.Combine(TokenTreeValidator));
        }

        [ImportMany]
        public IEnumerable<IMarkdownTokenTreeValidator> TokenTreeValidator { get; set; }

        private sealed class DfmService : IMarkdownService
        {
            private readonly DfmEngineBuilder _builder;

            private readonly ImmutableDictionary<string, string> _tokens;

            public DfmService(string baseDir, ImmutableDictionary<string, string> tokens, IMarkdownTokenTreeValidator tokenTreeValidator)
            {
                _builder = DocfxFlavoredMarked.CreateBuilder(baseDir);
                _builder.TokenTreeValidator = tokenTreeValidator;
                _tokens = tokens;
            }

            public MarkupResult Markup(string src, string path)
            {
                var dependency = new HashSet<string>();
                var html = _builder.CreateDfmEngine(new DfmRenderer() { Tokens = _tokens }).Markup(src, path, dependency);
                var result = new MarkupResult
                {
                    Html = html,
                };
                if (dependency.Count > 0)
                {
                    result.Dependency = dependency.ToImmutableArray();
                }
                return result;
            }
        }
    }
}
