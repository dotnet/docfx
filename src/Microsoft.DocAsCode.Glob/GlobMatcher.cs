// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Glob
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Linq;

    using Microsoft.DocAsCode.Common;

    [Serializable]
    public class GlobMatcher : IEquatable<GlobMatcher>
    {
        #region Private fields
        private static readonly StringComparer Comparer = FilePathComparer.OSPlatformSensitiveStringComparer;
        private static readonly string[] EmptyString = new string[0];
        private const char NegateChar = '!';
        private const string GlobStar = "**";
        private const string ReplacerGroupName = "replacer";
        private static readonly HashSet<char> NeedEscapeCharactersInRegex = new HashSet<char>(@"'().*{}+?[]^$\!".ToCharArray());
        private static readonly Regex UnescapeGlobRegex = new Regex(@"\\(?<replacer>.)", RegexOptions.Compiled);

        /// <summary>
        /// start with * and has more than one * and followed by anything except * or /
        /// </summary>
        private static readonly Regex ExpandGlobStarRegex = new Regex(@"^\*{2,}(?=[^/*])", RegexOptions.Compiled);
        // Never match .abc file unless AllowDotMatch option is set
        private const string PatternStartWithDotAllowed = @"(?!(?:^|\/)\.{1,2}(?:$|\/))";
        private const string PatternStartWithoutDotAllowed = @"(?!\.)";
        private static readonly HashSet<char> RegexCharactersWithDotPossible = new HashSet<char>(new char[] { '.', '[', '(' });
        /// <summary>
        /// Any character other than /
        /// </summary>
        private const string QuestionMarkToRegex = "[^/]";

        /// <summary>
        /// Any number of character other than /, non-greedy mode
        /// </summary>
        private const string SingleStarToRegex = "[^/]*?";

        private static readonly Regex GlobStarRegex = new Regex(@"^\*{2,}/?$", RegexOptions.Compiled);

        private GlobRegexItem[][] _items;
        private bool _negate = false;
        private bool _ignoreCase = false;
        #endregion

        public const GlobMatcherOptions DefaultOptions = GlobMatcherOptions.AllowNegate | GlobMatcherOptions.IgnoreCase | GlobMatcherOptions.AllowGlobStar | GlobMatcherOptions.AllowExpand | GlobMatcherOptions.AllowEscape;
        public GlobMatcherOptions Options { get; }
        public string Raw { get; }

        public GlobMatcher(string pattern, GlobMatcherOptions options = DefaultOptions)
        {
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));
            Options = options;
            Raw = pattern;
            _ignoreCase = Options.HasFlag(GlobMatcherOptions.IgnoreCase);
            _negate = ParseNegate(ref pattern, Options);
            _items = Compile(pattern).ToArray();
        }

        /// <summary>
        /// Currently not used
        /// TODO: add test case
        /// </summary>
        /// <param name="glob"></param>
        /// <returns></returns>
        public Regex GetRegex()
        {
            var regexParts = _items.Select(s => ConvertSingleGlob(s));
            var content = string.Join("|", regexParts);
            // Matches the entire pattern
            content = $"^(?:{content})$";
            if (_negate)
            {
                // Matches whatever not current pattern
                content = $"^(?!{content}).*$";
            }

            if (_ignoreCase)
            {
                return new Regex(content, RegexOptions.IgnoreCase);
            }
            else
            {
                return new Regex(content);
            }
        }

        public bool Match(string file, bool partial = false)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            var fileParts = Split(file, '/', '\\').ToArray();
            bool isMatch = false;
            foreach(var glob in _items)
            {
                if (MatchOne(fileParts, glob, partial))
                {
                    isMatch = true;
                    break;
                }
            }
            return _negate ^ isMatch;
        }

        #region Private methods

        private IEnumerable<GlobRegexItem[]> Compile(string pattern)
        {
            string[] globs;

            if (Options.HasFlag(GlobMatcherOptions.AllowExpand))
            {
                globs = ExpandGroup(pattern, Options);
                if (globs.Length == 0) return Enumerable.Empty<GlobRegexItem[]>();
            }
            else
            {
                globs = new string[] { pattern };
            }

            // **.cs is a shortcut for **/*.cs
            var items = globs
                .Select(glob => ExpandGlobStarShortcut(Split(glob, '/')).Select(s => ConvertSingleGlobPart(s)).ToArray());
            return items;
        }

        private IEnumerable<string> Split(string path, params char[] splitter)
        {
            var parts = path.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) yield break;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                yield return parts[i] + "/";
            }

            yield return path.EndsWith("/", StringComparison.Ordinal) ? parts[parts.Length - 1] + "/" : parts[parts.Length - 1];
        }

        private string ConvertSingleGlob(IEnumerable<GlobRegexItem> regexItems)
        {
            var items = regexItems.Select(s => GlobRegexItemToRegex(s));
            return string.Join(@"\/", items);
        }

        private bool IsFolderPath(string path)
        {
            return path.EndsWith("/", StringComparison.Ordinal);
        }

        /// <summary>
        /// Convert each part to Regex
        /// </summary>
        /// <param name="globPart">Part of glob that does not contain '/'</param>
        /// <returns></returns>
        private GlobRegexItem ConvertSingleGlobPart(string globPart)
        {
            // Return GlobStar for **
            if (Options.HasFlag(GlobMatcherOptions.AllowGlobStar) && GlobStarRegex.IsMatch(globPart))
            {
                return IsFolderPath(globPart) ? GlobRegexItem.GlobStar : GlobRegexItem.GlobStarForFileOnly;
            }

            StringBuilder builder = new StringBuilder();
            bool escaping = false;
            bool disableEscape = !Options.HasFlag(GlobMatcherOptions.AllowEscape);
            bool hasMagic = false;
            CharClass currentCharClass = null;
            string patternStart = string.Empty;

            // .abc will not be matched unless . is explictly specified
            if (globPart.Length > 0 && globPart[0] != '.')
            {
                patternStart = Options.HasFlag(GlobMatcherOptions.AllowDotMatch) ? PatternStartWithDotAllowed : PatternStartWithoutDotAllowed;
            }

            for (int i = 0; i < globPart.Length; i++)
            {
                var c = globPart[i];
                switch (c)
                {
                    case '\\':
                        if (!disableEscape)
                        {
                            i++;
                            if (i == globPart.Length)
                            {
                                // \ at the end of path part, invalid, not possible for file path, invalid
                                return GlobRegexItem.Empty;
                            }
                            else
                            {
                                c = globPart[i];
                                if (NeedEscapeCharactersInRegex.Contains(c))
                                {
                                    builder.Append('\\');
                                }
                                builder.Append(c);
                            }
                        }
                        else
                        {
                            builder.Append("\\\\");
                        }
                        break;
                    case '?':
                        builder.Append(QuestionMarkToRegex);
                        hasMagic = true;
                        break;
                    case '*':
                        builder.Append(SingleStarToRegex);
                        hasMagic = true;
                        break;
                    case '[':
                        escaping = false;
                        currentCharClass = new CharClass();
                        int cur = i + 1;
                        while (cur < globPart.Length)
                        {
                            c = globPart[cur];
                            if (c == '\\') escaping = true;
                            else if (c == ']' && !escaping)
                            {
                                // current char class ends when meeting the first non-escaping ]
                                builder.Append(currentCharClass.ToString());
                                currentCharClass = null;
                                break;
                            }

                            // simply keeps what it is inside char class
                            currentCharClass.Add(c);
                            if (c != '\\') escaping = false;
                            cur++;
                        }
                        if (currentCharClass != null)
                        {
                            // no closing ] is found, fallback to no char class
                            builder.Append("\\[");
                        }
                        else
                        {
                            i = cur;
                            hasMagic = true;
                        }
                        break;
                    default:
                        if (NeedEscapeCharactersInRegex.Contains(c))
                        {
                            builder.Append('\\');
                        }

                        builder.Append(c);
                        break;
                }
            }

            if (hasMagic)
            {
                var regexContent = builder.ToString();
                if (!string.IsNullOrEmpty(regexContent))
                {
                    // when regex is not empty, make sure it does not match against empty path, e.g. a/* should not match a/
                    // regex: if followed by anything
                    regexContent = "(?=.)" + regexContent;
                }
                else
                {
                    return GlobRegexItem.Empty;
                }

                if (RegexCharactersWithDotPossible.Contains(regexContent[0]))
                {
                    regexContent = patternStart + regexContent;
                }
                return new GlobRegexItem(regexContent, null, GlobRegexItemType.Regex, _ignoreCase);
            }
            else
            {
                // If does not contain any regex character, use the original string for regex
                // use escaped string as the string to be matched
                string plainText = UnescapeGlob(globPart);
                return new GlobRegexItem(globPart, plainText, GlobRegexItemType.PlainText, _ignoreCase);
            }
        }

        private string GlobRegexItemToRegex(GlobRegexItem item)
        {
            switch (item.ItemType)
            {
                case GlobRegexItemType.GlobStar:
                case GlobRegexItemType.GlobStarForFileOnly:
                    // If globstar is disabled
                    if (!Options.HasFlag(GlobMatcherOptions.AllowGlobStar))
                    {
                        return SingleStarToRegex;
                    }
                    if (Options.HasFlag(GlobMatcherOptions.AllowDotMatch))
                    {
                        // ** when dots are allowed, allows anything except .. and .
                        // not (^ or / followed by one or two dots followed by $ or /)
                        return @"(?:(?!(?:\/|^)(?:\.{1,2})($|\/)).)*?";
                    }
                    else
                    {
                        // not (^ or / followed by a dot)
                        return @"(?:(?!(?:\/|^)\.).)*?";
                    }
                case GlobRegexItemType.PlainText:
                case GlobRegexItemType.Regex:
                    return item.RegexContent;
                default:
                    throw new NotSupportedException($"{item.ItemType} is not current supported.");
            }
        }

        /// <summary>
        /// ** matches everything including "/" only when ** is after / or is the start of the pattern
        /// ** between characters has the same meaning as *
        /// **.cs equals to **/*.cs
        /// a**.cs equals to a*.cs
        /// </summary>
        /// <param name="globParts"></param>
        /// <returns></returns>
        private IEnumerable<string> ExpandGlobStarShortcut(IEnumerable<string> globParts)
        {
            foreach(var part in globParts)
            {
                if (ExpandGlobStarRegex.IsMatch(part))
                {
                    yield return GlobStar + "/";
                    yield return ExpandGlobStarRegex.Replace(part, "*");
                }
                else
                {
                    yield return part;
                }
            }
        }

        private bool MatchOne(string[] fileParts, GlobRegexItem[] globParts, bool matchPartialGlob)
        {
            bool[,] status = new bool[2, globParts.Length + 1];
            int prev = 0;
            int cur = 1;
            status[0, 0] = true;
            for (int j = 0; j < globParts.Length; j++)
            {
                if (matchPartialGlob)
                {
                    status[0, j + 1] = true;
                }
                else
                {
                    var globPart = globParts[globParts.Length - j - 1];
                    if (globPart.ItemType == GlobRegexItemType.GlobStar) status[0, j + 1] = status[0, j];
                    else status[0, j + 1] = false;
                }
            }

            for(int i = 0; i < fileParts.Length; i++)
            {
                status[cur, 0] = false;
                for (int j = 0; j < globParts.Length; j++)
                {
                    var filePart = fileParts[fileParts.Length - i - 1];
                    var globPart = globParts[globParts.Length - j - 1];
                    switch (globPart.ItemType)
                    {
                        case GlobRegexItemType.GlobStar:
                            if (DisallowedMatchExists(filePart)) status[cur, j + 1] = false;
                            else
                            {
                                var isFolderPath = IsFolderPath(filePart);
                                status[cur, j + 1] = (status[prev, j + 1] && isFolderPath) || (status[prev, j] && isFolderPath || status[cur, j]);
                            }
                            break;
                        case GlobRegexItemType.GlobStarForFileOnly:
                            if (DisallowedMatchExists(filePart)) status[cur, j + 1] = false;
                            else
                            {
                                var isFolderPath = IsFolderPath(filePart);
                                status[cur, j + 1] = status[prev, j + 1] || (status[prev, j] && !isFolderPath);
                            }
                            break;
                        case GlobRegexItemType.PlainText:
                            StringComparison comparison = StringComparison.Ordinal;
                            if (Options.HasFlag(GlobMatcherOptions.IgnoreCase))
                            {
                                comparison = StringComparison.OrdinalIgnoreCase;
                            }
                            status[cur, j + 1] = string.Equals(filePart, globPart.PlainText, comparison) && status[prev, j];
                            break;
                        case GlobRegexItemType.Regex:
                            status[cur, j + 1] = globPart.Regex.IsMatch(filePart) && status[prev, j];
                            break;
                    }
                }

                prev ^= 1;
                cur ^= 1;
            }

            return status[prev, globParts.Length];
        }
        
        private bool DisallowedMatchExists(string filePart)
        {
            if (filePart == "."
                || filePart == ".."
                || (!Options.HasFlag(GlobMatcherOptions.AllowDotMatch) && filePart.StartsWith(".", StringComparison.Ordinal)))
            {
                return true;
            }

            return false;
        }

        private static string UnescapeGlob(string s)
        {
            return UnescapeGlobRegex.Replace(s, new MatchEvaluator(ReplaceReplacerGroup));
        }

        private static string ReplaceReplacerGroup(Match m)
        {
            if (m.Success)
            {
                return m.Groups[ReplacerGroupName].Value;
            }
            return m.Value;
        }
        #endregion

        internal static bool ParseNegate(ref string pattern, GlobMatcherOptions options = DefaultOptions)
        {
            if (!options.HasFlag(GlobMatcherOptions.AllowNegate))
            {
                return false;
            }

            bool negate = false;
            int i = 0;
            while (i < pattern.Length && pattern[i] == NegateChar)
            {
                negate = !negate;
                i++;
            }

            if (i <= pattern.Length)
            {
                pattern = pattern.Substring(i);
            }

            return negate;
        }

        /// <summary>
        /// {a,b}c => [ac, bc]
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        internal static string[] ExpandGroup(string pattern, GlobMatcherOptions options = DefaultOptions)
        {
            GlobUngrouper ungrouper = new GlobUngrouper();
            bool escaping = false;
            bool disableEscape = !options.HasFlag(GlobMatcherOptions.AllowEscape);
            foreach (char c in pattern)
            {
                if (escaping)
                {
                    if (c != ',' && c != '{' && c != '}')
                    {
                        ungrouper.AddChar('\\');
                    }
                    ungrouper.AddChar(c);
                    escaping = false;
                    continue;
                }
                else if (c == '\\' && !disableEscape)
                {
                    escaping = true;
                    continue;
                }
                switch (c)
                {
                    case '{':
                        ungrouper.StartLevel();
                        break;
                    case ',':
                        if (ungrouper.Level < 1)
                        {
                            ungrouper.AddChar(c);
                        }
                        else
                        {
                            ungrouper.AddGroup();
                        }
                        break;
                    case '}':
                        if (ungrouper.Level < 1)
                        {
                            // Unbalanced closing bracket matches nothing
                            return EmptyString;
                        }
                        ungrouper.FinishLevel();
                        break;
                    default:
                        ungrouper.AddChar(c);
                        break;
                }
            }
            return ungrouper.Flatten();
        }

        #region Private classes
        private sealed class GlobUngrouper
        {
            public abstract class GlobNode
            {
                public readonly GlobNode _parent;
                protected GlobNode(GlobNode parentNode)
                {
                    _parent = parentNode ?? this;
                }
                abstract public GlobNode AddChar(char c);
                abstract public GlobNode StartLevel();
                abstract public GlobNode AddGroup();
                abstract public GlobNode FinishLevel();
                abstract public List<StringBuilder> Flatten();
            }
            public class TextNode : GlobNode
            {
                private readonly StringBuilder _builder;
                public TextNode(GlobNode parentNode)
                : base(parentNode)
                {
                    _builder = new StringBuilder();
                }
                public override GlobNode AddChar(char c)
                {
                    if (c != 0)
                    {
                        _builder.Append(c);
                    }
                    return this;
                }
                public override GlobNode StartLevel()
                {
                    return _parent.StartLevel();
                }
                public override GlobNode AddGroup()
                {
                    return _parent.AddGroup();
                }
                public override GlobNode FinishLevel()
                {
                    return _parent.FinishLevel();
                }
                public override List<StringBuilder> Flatten()
                {
                    List<StringBuilder> result = new List<StringBuilder>(1);
                    result.Add(_builder);
                    return result;
                }
            }
            public class ChoiceNode : GlobNode
            {
                private readonly List<SequenceNode> _nodes;
                public ChoiceNode(GlobNode parentNode)
                : base(parentNode)
                {
                    _nodes = new List<SequenceNode>();
                }
                public override GlobNode AddChar(char c)
                {
                    SequenceNode node = new SequenceNode(this);
                    _nodes.Add(node);
                    return node.AddChar(c);
                }
                public override GlobNode StartLevel()
                {
                    SequenceNode node = new SequenceNode(this);
                    _nodes.Add(node);
                    return node.StartLevel();
                }
                public override GlobNode AddGroup()
                {
                    return AddChar('\0');
                }
                public override GlobNode FinishLevel()
                {
                    AddChar('\0');
                    return _parent;
                }
                public override List<StringBuilder> Flatten()
                {
                    List<StringBuilder> result = new List<StringBuilder>();
                    foreach (GlobNode node in _nodes)
                    {
                        foreach (StringBuilder builder in node.Flatten())
                        {
                            result.Add(builder);
                        }
                    }
                    return result;
                }
            }
            public class SequenceNode : GlobNode
            {
                private readonly List<GlobNode> _nodes;
                public SequenceNode(GlobNode parentNode)
                : base(parentNode)
                {
                    _nodes = new List<GlobNode>();
                }
                public override GlobNode AddChar(char c)
                {
                    TextNode node = new TextNode(this);
                    _nodes.Add(node);
                    return node.AddChar(c);
                }
                public override GlobNode StartLevel()
                {
                    ChoiceNode node = new ChoiceNode(this);
                    _nodes.Add(node);
                    return node;
                }
                public override GlobNode AddGroup()
                {
                    return _parent;
                }
                public override GlobNode FinishLevel()
                {
                    return _parent._parent;
                }
                public override List<StringBuilder> Flatten()
                {
                    List<StringBuilder> result = new List<StringBuilder>();
                    result.Add(new StringBuilder());
                    foreach (GlobNode node in _nodes)
                    {
                        List<StringBuilder> tmp = new List<StringBuilder>();
                        foreach (StringBuilder builder in node.Flatten())
                        {
                            foreach (StringBuilder sb in result)
                            {
                                StringBuilder newsb = new StringBuilder(sb.ToString());
                                newsb.Append(builder.ToString());
                                tmp.Add(newsb);
                            }
                        }
                        result = tmp;
                    }
                    return result;
                }
            }
            private readonly SequenceNode _rootNode;
            private GlobNode _currentNode;
            private int _level;
            public GlobUngrouper()
            {
                _rootNode = new SequenceNode(null);
                _currentNode = _rootNode;
                _level = 0;
            }
            public void AddChar(char c)
            {
                _currentNode = _currentNode.AddChar(c);
            }
            public void StartLevel()
            {
                _currentNode = _currentNode.StartLevel();
                _level++;
            }
            public void AddGroup()
            {
                _currentNode = _currentNode.AddGroup();
            }
            public void FinishLevel()
            {
                _currentNode = _currentNode.FinishLevel();
                _level--;
            }
            public int Level
            {
                get { return _level; }
            }
            public string[] Flatten()
            {
                if (_level != 0)
                {
                    return EmptyString;
                }
                List<StringBuilder> list = _rootNode.Flatten();
                string[] result = new string[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    result[i] = list[i].ToString();
                }
                return result;
            }
        }

        /// <summary>
        /// Represents [] class
        /// </summary>
        private sealed class CharClass
        {
            private readonly StringBuilder _chars = new StringBuilder();
            public void Add(char c)
            {
                _chars.Append(c);
            }

            public override string ToString()
            {
                if (_chars.Length == 0)
                {
                    return string.Empty;
                }
                if (_chars.Length == 1 && _chars[0] == '^')
                {
                    _chars.Insert(0, "\\");
                }
                return $"[{_chars.ToString()}]";
            }
        }

        [Serializable]
        private sealed class GlobRegexItem
        {
            public static readonly GlobRegexItem GlobStar = new GlobRegexItem(GlobRegexItemType.GlobStar);
            public static readonly GlobRegexItem GlobStarForFileOnly = new GlobRegexItem(GlobRegexItemType.GlobStarForFileOnly);
            public static readonly GlobRegexItem Empty = new GlobRegexItem(string.Empty, string.Empty, GlobRegexItemType.PlainText);
            public GlobRegexItemType ItemType { get; }
            public string RegexContent { get; }
            public string PlainText { get; }
            public Regex Regex { get; }
            public GlobRegexItem(string content, string plainText, GlobRegexItemType type, bool ignoreCase = true)
            {
                RegexContent = content;
                ItemType = type;
                PlainText = plainText;
                if (type == GlobRegexItemType.Regex)
                {
                    var regexSegment = $"^{RegexContent}$";
                    Regex = ignoreCase ? new Regex(regexSegment, RegexOptions.IgnoreCase) : new Regex(regexSegment);
                }
            }

            private GlobRegexItem(GlobRegexItemType itemType)
            {
                ItemType = itemType;
            }
        }

        private enum GlobRegexItemType
        {
            GlobStarForFileOnly, // ** to match files only
            GlobStar, // **/ to match files or folders
            PlainText,
            Regex,
        }
        #endregion

        #region Compare & Equatable

        public bool Equals(GlobMatcher other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Comparer.Equals(Raw, other.Raw) && Options == other.Options;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as GlobMatcher);
        }

        public override int GetHashCode()
        {
            return Options.GetHashCode() ^ (Comparer.GetHashCode(Raw) >> 1);
        }

        #endregion
    }

    [Flags]
    public enum GlobMatcherOptions
    {
        None = 0x0,
        IgnoreCase = 0x1,
        AllowNegate = 0x2,
        AllowExpand = 0x4,
        AllowEscape = 0x8,
        AllowGlobStar = 0x10,
        /// <summary>
        /// Allow patterns to match filenames starting with a period even if the pattern does not explicitly have a period.
        /// By default disabled: a/**/b will **not** match a/.c/d, unless `AllowDotMatch` is set
        /// </summary>
        AllowDotMatch = 0x20,
    }
}