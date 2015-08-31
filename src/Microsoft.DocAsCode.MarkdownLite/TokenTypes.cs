// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class TokenTypes
    {

        private static readonly ImmutableDictionary<string, TokenType> DefaultTypes;
        public static readonly TokenTypes Default;
        public static readonly TokenType Space;
        public static readonly TokenType Hr;
        public static readonly TokenType Heading;
        public static readonly TokenType Code;
        public static readonly TokenType Table;
        public static readonly TokenType BlockquoteStart;
        public static readonly TokenType BlockquoteEnd;
        public static readonly TokenType ListStart;
        public static readonly TokenType ListEnd;
        public static readonly TokenType ListItemStart;
        public static readonly TokenType ListItemEnd;
        public static readonly TokenType LooseItemStart;
        public static readonly TokenType Html;
        public static readonly TokenType Paragraph;
        public static readonly TokenType Text;

        public readonly ImmutableDictionary<string, TokenType> _types;

        static TokenTypes()
        {
            var builder = ImmutableDictionary<string, TokenType>.Empty.ToBuilder();

            Space = new TokenType("space");
            builder.Add("space", Space);

            Hr = new TokenType("hr");
            builder.Add("hr", Hr);

            Heading = new TokenType("heading");
            builder.Add("heading", Heading);

            Code = new TokenType("code");
            builder.Add("code", Code);

            Table = new TokenType("table");
            builder.Add("table", Table);

            BlockquoteStart = new TokenType("blockquote_start");
            builder.Add("blockquote_start", BlockquoteStart);

            BlockquoteEnd = new TokenType("blockquote_end");
            builder.Add("blockquote_end", BlockquoteEnd);

            ListStart = new TokenType("list_start");
            builder.Add("list_start", ListStart);

            ListEnd = new TokenType("list_end");
            builder.Add("list_end", ListEnd);

            ListItemStart = new TokenType("list_item_start");
            builder.Add("list_item_start", ListItemStart);

            ListItemEnd = new TokenType("list_item_end");
            builder.Add("list_item_end", ListItemEnd);

            LooseItemStart = new TokenType("loose_item_start");
            builder.Add("loose_item_start", LooseItemStart);

            Html = new TokenType("html");
            builder.Add("html", Html);

            Paragraph = new TokenType("paragraph");
            builder.Add("paragraph", Paragraph);

            Text = new TokenType("text");
            builder.Add("text", Text);

            DefaultTypes = builder.ToImmutableDictionary();

            Default = new TokenTypes(DefaultTypes);
        }

        private TokenTypes(ImmutableDictionary<string, TokenType> types)
        {
            _types = types;
        }

        public static TokenTypesBuilder CreateExtensionBuilder()
        {
            return new TokenTypesBuilder(DefaultTypes.ToBuilder());
        }

        public TokenType this[string type]
        {
            get { return _types[type]; }
        }

        public class TokenTypesBuilder
        {
            private readonly ImmutableDictionary<string, TokenType>.Builder _builder;

            internal TokenTypesBuilder(ImmutableDictionary<string, TokenType>.Builder builder)
            {
                _builder = builder;
            }

            public bool Add(string type)
            {
                if (_builder.ContainsKey(type))
                {
                    return false;
                }
                _builder.Add(type, new TokenType(type));
                return true;
            }

            public bool Remove(string type)
            {
                return _builder.Remove(type);
            }

            public TokenTypes ToTokenTypes()
            {
                return new TokenTypes(_builder.ToImmutableDictionary());
            }

        }
    }

}
