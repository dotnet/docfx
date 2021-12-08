// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;

using Markdig.Helpers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.MarkdigExtensions;

public class HtmlCodeSnippetRenderer : HtmlObjectRenderer<CodeSnippet>
{
    private const string TagPrefix = "snippet";
    private const string WarningMessageId = "codeIncludeNotFound";
    private const string DefaultWarningMessage = "It looks like the sample you are looking for does not exist.";
    private const string WarningTitleId = "warning";
    private const string DefaultWarningTitle = "<h5>WARNING</h5>";

    // C# code snippet comment block: // <[/]snippetname>
    private const string CFamilyCodeSnippetCommentStartLineTemplate = "//<{tagname}>";
    private const string CFamilyCodeSnippetCommentEndLineTemplate = "//</{tagname}>";

    // C# code snippet region block: start -> #region snippetname, end -> #endregion
    private const string CSharpCodeSnippetRegionStartLineTemplate = "#region{tagname}";
    private const string CSharpCodeSnippetRegionEndLineTemplate = "#endregion";

    // VB code snippet comment block: ' <[/]snippetname>
    private const string BasicFamilyCodeSnippetCommentStartLineTemplate = "'<{tagname}>";
    private const string BasicFamilyCodeSnippetCommentEndLineTemplate = "'</{tagname}>";

    // VB code snippet Region block: start -> # Region "snippetname", end -> # End Region
    private const string VBCodeSnippetRegionRegionStartLineTemplate = "#region{tagname}";
    private const string VBCodeSnippetRegionRegionEndLineTemplate = "#endregion";

    // XML code snippet block: <!-- <[/]snippetname> -->
    private const string MarkupLanguageFamilyCodeSnippetCommentStartLineTemplate = "<!--<{tagname}>-->";
    private const string MarkupLanguageFamilyCodeSnippetCommentEndLineTemplate = "<!--</{tagname}>-->";

    // Sql code snippet block: -- <[/]snippetname>
    private const string SqlFamilyCodeSnippetCommentStartLineTemplate = "--<{tagname}>";
    private const string SqlFamilyCodeSnippetCommentEndLineTemplate = "--</{tagname}>";

    // Python code snippet comment block: # <[/]snippetname>
    private const string ScriptFamilyCodeSnippetCommentStartLineTemplate = "#<{tagname}>";
    private const string ScriptFamilyCodeSnippetCommentEndLineTemplate = "#</{tagname}>";

    // Batch code snippet comment block: rem <[/]snippetname>
    private const string BatchFileCodeSnippetRegionStartLineTemplate = "rem<{tagname}>";
    private const string BatchFileCodeSnippetRegionEndLineTemplate = "rem</{tagname}>";

    // Erlang code snippet comment block: % <[/]snippetname>
    private const string ErlangCodeSnippetRegionStartLineTemplate = "%<{tagname}>";
    private const string ErlangCodeSnippetRegionEndLineTemplate = "%</{tagname}>";

    // Lisp code snippet comment block: ; <[/]snippetname>
    private const string LispCodeSnippetRegionStartLineTemplate = ";<{tagname}>";
    private const string LispCodeSnippetRegionEndLineTemplate = ";</{tagname}>";

    // css code snippet comment block: ; <[/]snippetname>
    private const string CSSCodeSnippetRegionStartLineTemplate = "/*<{tagname}>*/";
    private const string CSSCodeSnippetRegionEndLineTemplate = "/*</{tagname}>*/";

    private static readonly Dictionary<string, string[]> s_languageAlias = new()
    {
        { "actionscript", new string[] { "as" } },
        { "arduino", new string[] { "ino" } },
        { "assembly", new string[] { "nasm", "asm" } },
        { "batchfile", new string[] { "bat", "cmd" } },
        { "css", Array.Empty<string>() },
        { "cpp", new string[] { "c", "c++", "objective-c", "obj-c", "objc", "objectivec", "h", "hpp", "cc", "m" } },
        { "csharp", new string[] { "cs" } },
        { "cuda", new string[] { "cu", "cuh" } },
        { "d", new string[] { "dlang" } },
        { "everything", new string[] { "example" } }, // this is the catch all to try and process unforseen languages
        { "erlang", new string[] { "erl" } },
        { "fsharp", new string[] { "fs", "fsi", "fsx" } },
        { "go", new string[] { "golang" } },
        { "handlebars", new string[] { "hbs" } },
        { "haskell", new string[] { "hs" } },
        { "html", new string[] { "jsp", "asp", "aspx", "ascx" } },
        { "cshtml", new string[] { "aspx-cs", "aspx-csharp", "razor" } },
        { "vbhtml", new string[] { "aspx-vb" } },
        { "java", new string[] { "gradle" } },
        { "javascript", new string[] { "js", "node", "json" } },
        { "lisp", new string[] { "lsp" } },
        { "lua", Array.Empty<string>() },
        { "matlab", Array.Empty<string>() },
        { "pascal", new string[] { "pas" } },
        { "perl", new string[] { "pl" } },
        { "php", Array.Empty<string>() },
        { "powershell", new string[] { "posh", "ps1" } },
        { "processing", new string[] { "pde" } },
        { "python", new string[] { "py" } },
        { "r", Array.Empty<string>() },
        { "react", new string[] { "tsx" } },
        { "ruby", new string[] { "ru", "erb", "rb", "" } },
        { "rust", new string[] { "rs" } },
        { "scala", Array.Empty<string>() },
        { "shell", new string[] { "sh", "bash" } },
        { "smalltalk", new string[] { "st" } },
        { "sql", Array.Empty<string>() },
        { "swift", Array.Empty<string>() },
        { "typescript", new string[] { "ts" } },
        { "xaml", Array.Empty<string>() },
        { "xml", new string[] { "xsl", "xslt", "xsd", "wsdl", "csdl", "edmx", "vsixmanifest" } },
        { "vb", new string[] { "vbnet", "vbscript", "bas", "vbs", "vba" } },
    };

    private static readonly Dictionary<string, string> s_languageByFileExtension = new();

    // If we ever come across a language that has not been defined above, we shouldn't break the build.
    // We can at least try it with a default language, "C#" for now, and try and resolve the code snippet.
    private static readonly HashSet<CodeSnippetExtractor> s_defaultExtractors = new();

    // Language names and aliases follow http://highlightjs.readthedocs.org/en/latest/css-classes-reference.html#language-names-and-aliases
    // Language file extensions follow https://github.com/github/linguist/blob/master/lib/linguist/languages.yml
    // Currently only supports parts of the language names, aliases and extensions
    // Later we can move the repository's supported/custom language names, aliases,
    // extensions and corresponding comments regexes to docfx build configuration
    private static readonly Dictionary<string, HashSet<CodeSnippetExtractor>> s_languageExtractors = new();

    private readonly MarkdownContext _context;

    static HtmlCodeSnippetRenderer()
    {
        BuildFileExtensionLanguageMap();

        AddExtractorItems(
            new[] { "vb", "vbhtml" },
            new CodeSnippetExtractor(BasicFamilyCodeSnippetCommentStartLineTemplate, BasicFamilyCodeSnippetCommentEndLineTemplate));
        AddExtractorItems(
            new[]
            {
                    "actionscript", "arduino", "assembly", "cpp", "csharp", "cshtml", "cuda", "d", "fsharp", "go", "java", "javascript",
                    "objectivec", "pascal", "php", "processing", "react", "rust", "scala", "smalltalk", "swift", "typescript", "scss",
            },
            new CodeSnippetExtractor(CFamilyCodeSnippetCommentStartLineTemplate, CFamilyCodeSnippetCommentEndLineTemplate));
        AddExtractorItems(
            new[] { "xml", "xaml", "handlebars", "html", "cshtml", "php", "react", "ruby", "vbhtml" },
            new CodeSnippetExtractor(MarkupLanguageFamilyCodeSnippetCommentStartLineTemplate, MarkupLanguageFamilyCodeSnippetCommentEndLineTemplate));
        AddExtractorItems(
            new[] { "haskell", "lua", "sql" },
            new CodeSnippetExtractor(SqlFamilyCodeSnippetCommentStartLineTemplate, SqlFamilyCodeSnippetCommentEndLineTemplate));
        AddExtractorItems(
            new[] { "perl", "powershell", "python", "r", "ruby", "shell" },
            new CodeSnippetExtractor(ScriptFamilyCodeSnippetCommentStartLineTemplate, ScriptFamilyCodeSnippetCommentEndLineTemplate));
        AddExtractorItems(
            new[] { "batchfile" },
            new CodeSnippetExtractor(BatchFileCodeSnippetRegionStartLineTemplate, BatchFileCodeSnippetRegionEndLineTemplate));
        AddExtractorItems(
            new[] { "csharp", "cshtml" },
            new CodeSnippetExtractor(CSharpCodeSnippetRegionStartLineTemplate, CSharpCodeSnippetRegionEndLineTemplate, false));
        AddExtractorItems(
            new[] { "erlang", "matlab" },
            new CodeSnippetExtractor(ErlangCodeSnippetRegionStartLineTemplate, ErlangCodeSnippetRegionEndLineTemplate));
        AddExtractorItems(
            new[] { "lisp" },
            new CodeSnippetExtractor(LispCodeSnippetRegionStartLineTemplate, LispCodeSnippetRegionEndLineTemplate));
        AddExtractorItems(
            new[] { "vb", "vbhtml" },
            new CodeSnippetExtractor(VBCodeSnippetRegionRegionStartLineTemplate, VBCodeSnippetRegionRegionEndLineTemplate, false));
        AddExtractorItems(
            new[] { "css", "scss" },
            new CodeSnippetExtractor(CSSCodeSnippetRegionStartLineTemplate, CSSCodeSnippetRegionEndLineTemplate, false));

        static void BuildFileExtensionLanguageMap()
        {
            foreach (var (language, aliases) in s_languageAlias)
            {
                Debug.Assert(!language.StartsWith('.'));

                s_languageByFileExtension.Add(language, language);
                s_languageByFileExtension.Add($".{language}", language);

                foreach (var alias in aliases)
                {
                    Debug.Assert(!alias.StartsWith('.'));

                    s_languageByFileExtension.Add(alias, language);
                    s_languageByFileExtension.Add($".{alias}", language);
                }
            }
        }

        static void AddExtractorItems(string[] languages, CodeSnippetExtractor extractor)
        {
            s_defaultExtractors.Add(extractor);

            foreach (var language in languages)
            {
                AddExtractorItem(language, extractor);
                AddExtractorItem($".{language}", extractor);

                if (s_languageAlias.TryGetValue(language, out var aliases))
                {
                    foreach (var alias in aliases)
                    {
                        AddExtractorItem(alias, extractor);
                        AddExtractorItem($".{alias}", extractor);
                    }
                }
            }
        }

        static void AddExtractorItem(string language, CodeSnippetExtractor extractor)
        {
            if (s_languageExtractors.TryGetValue(language, out var extractors))
            {
                extractors.Add(extractor);
            }
            else
            {
                s_languageExtractors[language] = new() { extractor };
            }
        }
    }

    public HtmlCodeSnippetRenderer(MarkdownContext context)
    {
        _context = context;
    }

    public static string GetLanguageByFileExtension(string extension)
    {
        return s_languageByFileExtension.TryGetValue(extension, out var result) ? result : null;
    }

    public string GetContent(string content, CodeSnippet obj, MarkdownObject source)
    {
        var allLines = ReadAllLines(content).ToArray();

        // code range priority: tag > #L1 > start/end > range > default
        if (!string.IsNullOrEmpty(obj.TagName))
        {
            var lang = obj.Language ?? Path.GetExtension(obj.CodePath);

            if (obj.IsNotebookCode)
            {
                return GetNoteBookContent(content, obj.TagName, obj);
            }

            if (!s_languageExtractors.TryGetValue(lang, out var extractors))
            {
                extractors = s_defaultExtractors;

                _context.LogWarning(
                    "unknown-language-code",
                    $"Unrecognized language value '{lang}' in code snippet '{obj.TagName}' in file '{obj.CodePath}'. Your code snippet might not render correctly. If this is the case, you can request a new value or use range instead.",
                    source);
            }

            var tagWithPrefix = TagPrefix + obj.TagName;
            foreach (var extractor in extractors)
            {
                var tagLines = new HashSet<int>();
                var tagToCoderangeMapping = extractor.GetAllTags(allLines, ref tagLines);
                if (tagToCoderangeMapping.TryGetValue(obj.TagName, out var cr)
                    || tagToCoderangeMapping.TryGetValue(tagWithPrefix, out cr))
                {
                    return GetCodeLines(allLines, obj, new List<CodeRange> { cr }, tagLines);
                }
            }
        }
        else if (obj.BookMarkRange != null)
        {
            return GetCodeLines(allLines, obj, new List<CodeRange> { obj.BookMarkRange });
        }
        else if (obj.StartEndRange != null)
        {
            return GetCodeLines(allLines, obj, new List<CodeRange> { obj.StartEndRange });
        }
        else if (obj.CodeRanges != null)
        {
            return GetCodeLines(allLines, obj, obj.CodeRanges);
        }
        else
        {
            return GetCodeLines(allLines, obj, new List<CodeRange> { new CodeRange { Start = 0, End = allLines.Length } });
        }

        return "";
    }

    protected override void Write(HtmlRenderer renderer, CodeSnippet codeSnippet)
    {
        var (content, codeSnippetPath) = _context.ReadFile(codeSnippet.CodePath, codeSnippet, true);

        if (content == null)
        {
            _context.LogWarning("codesnippet-not-found", $"Invalid code snippet link: '{codeSnippet.CodePath}'.", codeSnippet);
            renderer.Write(GetWarning());
            return;
        }

        codeSnippet.SetAttributeString();

        renderer.Write("<pre><code").WriteAttributes(codeSnippet).Write(">");
        renderer.WriteEscape(GetContent(content, codeSnippet, codeSnippet));
        renderer.Write("</code></pre>");
    }

    private string GetNoteBookContent(string content, string tagName, CodeSnippet obj)
    {
        JObject contentObject = null;
        try
        {
            contentObject = JObject.Parse(content);
        }
        catch (JsonReaderException ex)
        {
            _context.LogError("not-notebook-content", "Not a valid Notebook. " + ex.ToString(), obj);
            return "";
        }

        var sourceJsonPath = $"$..cells[?(@.metadata.name=='{tagName}')].source";
        JToken sourceObject = null;
        try
        {
            sourceObject = contentObject.SelectToken(sourceJsonPath);
        }
        catch (JsonException)
        {
            _context.LogError("multiple-tags-with-same-name", $"Multiple entries with the name '{tagName}' where found in the notebook.", obj);
            return "";
        }

        if (sourceObject == null)
        {
            _context.LogError("tag-not-found", $"The name '{tagName}' is not present in the notebook file.", obj);
            return "";
        }

        var showCode = new StringBuilder();
        var lines = ((JArray)sourceObject).ToObject<string[]>();
        for (var i = 0; i < lines.Length; i++)
        {
            showCode.Append(lines[i]);
        }

        return showCode.ToString();
    }

    private static IEnumerable<string> ReadAllLines(string content)
    {
        string line;
        var reader = new StringReader(content);
        while ((line = reader.ReadLine()) != null)
        {
            yield return line;
        }
    }

    private static string GetCodeLines(string[] allLines, CodeSnippet obj, List<CodeRange> codeRanges, HashSet<int> ignoreLines = null)
    {
        var codeLines = new List<string>();
        var showCode = new StringBuilder();
        var commonIndent = int.MaxValue;

        foreach (var codeRange in codeRanges)
        {
            for (var lineNumber = Math.Max(codeRange.Start - 1, 0); lineNumber < Math.Min(codeRange.End, allLines.Length); lineNumber++)
            {
                if (ignoreLines != null && ignoreLines.Contains(lineNumber))
                {
                    continue;
                }

                if (IsBlankLine(allLines[lineNumber]))
                {
                    codeLines.Add(allLines[lineNumber]);
                }
                else
                {
                    var rawCodeLine = CountAndReplaceIndentSpaces(allLines[lineNumber], out var indentSpaces);
                    commonIndent = Math.Min(commonIndent, indentSpaces);
                    codeLines.Add(rawCodeLine);
                }
            }
        }

        var dedent = obj.DedentLength == null || obj.DedentLength < 0 ? commonIndent : (int)obj.DedentLength;

        foreach (var rawCodeLine in codeLines)
        {
            showCode.Append($"{DedentString(rawCodeLine, dedent)}\n");
        }

        return showCode.ToString();
    }

    private static string DedentString(string source, int dedent)
    {
        var validDedent = Math.Min(dedent, source.Length);
        for (var i = 0; i < validDedent; i++)
        {
            if (source[i] != ' ')
            {
                return source[i..];
            }
        }
        return source[validDedent..];
    }

    private static bool IsBlankLine(string line)
    {
        return string.IsNullOrEmpty(line);
    }

    private static string CountAndReplaceIndentSpaces(string line, out int count)
    {
        var sb = new StringBuilder();
        count = 0;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == ' ')
            {
                sb.Append(' ');
                count++;
            }
            else if (c == '\t')
            {
                var newCount = CharHelper.AddTab(count);
                sb.Append(' ', newCount - count);
                count = newCount;
            }
            else
            {
                sb.Append(line, i, line.Length - i);
                break;
            }
        }

        return sb.ToString();
    }

    private string GetWarning()
    {
        var warningTitle = _context.GetToken(WarningTitleId) ?? DefaultWarningTitle;
        var warningMessage = _context.GetToken(WarningMessageId) ?? DefaultWarningMessage;

        return $@"<div class=""WARNING"">
{warningTitle}
<p>{warningMessage}</p>
</div>";
    }

    public static bool TryGetLineRanges(string query, out List<CodeRange> codeRanges)
    {
        codeRanges = null;
        if (string.IsNullOrEmpty(query))
        {
            return false;
        }

        var rangesSplit = query.Split(new[] { ',' });

        foreach (var range in rangesSplit)
        {
            if (!TryGetLineRange(range, out var codeRange, false))
            {
                return false;
            }

            if (codeRanges == null)
            {
                codeRanges = new();
            }

            codeRanges.Add(codeRange);
        }

        return true;
    }

    public static bool TryGetLineRange(string query, out CodeRange codeRange, bool withL = true)
    {
        codeRange = null;
        if (string.IsNullOrEmpty(query))
        {
            return false;
        }

        int endLine;

        var splitLine = query.Split(new[] { '-' });
        if (splitLine.Length > 2)
        {
            return false;
        }

        var result = TryGetLineNumber(splitLine[0], out var startLine, withL);
        endLine = startLine;

        if (splitLine.Length > 1)
        {
            result &= TryGetLineNumber(splitLine[1], out endLine, withL);
        }

        codeRange = new CodeRange { Start = startLine, End = endLine };

        return result;
    }

    public static bool TryGetLineNumber(string lineNumberString, out int lineNumber, bool withL = true)
    {
        lineNumber = int.MaxValue;
        if (string.IsNullOrEmpty(lineNumberString))
        {
            return true;
        }

        if (withL && (lineNumberString.Length < 2 || char.ToUpperInvariant(lineNumberString[0]) != 'L'))
        {
            return false;
        }

        return int.TryParse(withL ? lineNumberString[1..] : lineNumberString, out lineNumber);
    }
}
