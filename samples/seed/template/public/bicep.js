/**
 Origin: https://github.com/Duncanma/highlight.js/blob/stable/src/languages/bicep.js
 */

export function bicep(hljs) {
    var bounded = function (text) { return "\\b" + text + "\\b"; };
    var after = function (regex) { return "(?<=" + regex + ")"; };
    var notAfter = function (regex) { return "(?<!" + regex + ")"; };
    var before = function (regex) { return "(?=" + regex + ")"; };
    var notBefore = function (regex) { return "(?!" + regex + ")"; };
    var identifierStart = "[_a-zA-Z]";
    var identifierContinue = "[_a-zA-Z0-9]";
    var identifier = bounded("" + identifierStart + identifierContinue + "*");
    // whitespace. ideally we'd tokenize in-line block comments, but that's a lot of work. For now, ignore them.
    var ws = "(?:[ \\t\\r\\n]|\\/\\*(?:\\*(?!\\/)|[^*])*\\*\\/)*";
    var KEYWORDS = {
        $pattern: '[A-Za-z$_][0-9A-Za-z$_]*',
        keyword: [
            'targetScope',
            'resource',
            'module',
            'param',
            'var',
            'output',
            'for',
            'in',
            'if',
            'existing',
        ].join(' '),
        literal: [
            "true",
            "false",
            "null",
        ].join(' '),
        built_in: [
            'az',
            'sys',
        ].join(' ')
    };
    var lineComment = {
        className: 'comment',
        begin: "//",
        end: "$",
        relevance: 0,
    };
    var blockComment = {
        className: 'comment',
        begin: "/\\*",
        end: "\\*/",
        relevance: 0,
    };
    var comments = {
        variants: [lineComment, blockComment],
    };
    function withComments(input) {
        return input.concat(comments);
    }
    var expression = {
        keywords: KEYWORDS,
        variants: [
        /* placeholder filled later due to cycle*/
        ],
    };
    var escapeChar = {
        begin: "\\\\(u{[0-9A-Fa-f]+}|n|r|t|\\\\|'|\\${)",
    };
    var stringVerbatim = {
        className: 'string',
        begin: "'''",
        end: "'''",
    };
    var stringSubstitution = {
        className: 'subst',
        begin: "(\\${)",
        end: "(})",
        contains: withComments([expression]),
    };
    var stringLiteral = {
        className: 'string',
        begin: "'" + notBefore("''"),
        end: "'",
        contains: [
            escapeChar,
            stringSubstitution
        ],
    };
    var numericLiteral = {
        className: "number",
        begin: "[0-9]+",
    };
    var namedLiteral = {
        className: 'literal',
        begin: bounded("(true|false|null)"),
        relevance: 0,
    };
    var identifierExpression = {
        className: "variable",
        begin: "" + identifier + notBefore(ws + "\\("),
    };
    var objectPropertyKeyIdentifier = {
        className: "property",
        begin: "(" + identifier + ")",
    };
    var objectProperty = {
        variants: [
            objectPropertyKeyIdentifier,
            stringLiteral,
            {
                begin: ":" + ws,
                excludeBegin: true,
                end: ws + "$",
                excludeEnd: true,
                contains: withComments([expression]),
            },
        ],
    };
    var objectLiteral = {
        begin: "{",
        end: "}",
        contains: withComments([objectProperty]),
    };
    var arrayLiteral = {
        begin: "\\[" + notBefore("" + ws + bounded("for")),
        end: "]",
        contains: withComments([expression]),
    };
    var functionCall = {
        className: 'function',
        begin: "(" + identifier + ")" + ws + "\\(",
        end: "\\)",
        contains: withComments([expression]),
    };
    var decorator = {
        className: 'meta',
        begin: "@" + ws + before(identifier),
        end: "",
        contains: withComments([functionCall]),
    };
    expression.variants = [
        stringLiteral,
        stringVerbatim,
        numericLiteral,
        namedLiteral,
        objectLiteral,
        arrayLiteral,
        identifierExpression,
        functionCall,
        decorator,
    ];
    return {
        aliases: ['bicep'],
        case_insensitive: true,
        keywords: KEYWORDS,
        contains: withComments([expression]),
    };
}
