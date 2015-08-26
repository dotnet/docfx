namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    public class RelativePathRewriter
    {
        private static readonly Tuple<Regex, int>[] RegexAndGroupIndexCollection =
            new[]
            {
                Tuple.Create(
                    new Regex(
                        AddPartPrefix(
                            @"\[.+?\]\((.+?)(\s+\""(.+?)\"")?\)",
                            @"(?<!\\)",
                            @"\[",
                            @"\]",
                            @"\(",
                            @"\)"),
                        RegexOptions.Compiled),
                    1),
                Tuple.Create(new Regex(@"\bhref\s*=\s*\""(.+?)\""", RegexOptions.Compiled | RegexOptions.IgnoreCase), 1),
                Tuple.Create(new Regex(@"\bhref\s*=\s*\'(.+?)\'", RegexOptions.Compiled | RegexOptions.IgnoreCase), 1),
                Tuple.Create(new Regex(@"\bsrc\s*=\s*\""(.+?)\""", RegexOptions.Compiled | RegexOptions.IgnoreCase), 1),
                Tuple.Create(new Regex(@"\bsrc\s*=\s*\'(.+?)\'", RegexOptions.Compiled | RegexOptions.IgnoreCase), 1),
            };

        public static object Rewrite(object value, RelativePath from, RelativePath to)
        {
            return RewriteCore(
                value,
                GetRewriteFunc(from, to),
                new RewriteContext(ParentType.NoParent, null));
        }

        private static string AddPartPrefix(string text, string partPrefix, params string[] parts)
        {
            foreach (var part in parts)
            {
                text = text.Replace(part, partPrefix + part);
            }
            return text;
        }

        private static object RewriteCore(object value, Func<string, RewriteContext, string> func, RewriteContext context)
        {
            var str = value as string;
            if (str != null)
            {
                return func(str, context);
            }
            var list = value as List<object>;
            if (list != null)
            {
                var result = new List<object>();
                foreach (var item in list)
                {
                    result.Add(RewriteCore(item, func, new RewriteContext(ParentType.List, null)));
                }
                return result;
            }
            var dict = value as Dictionary<object, object>;
            if (dict != null)
            {
                var result = new Dictionary<object, object>();
                foreach (var pair in dict)
                {
                    result.Add(pair.Key, RewriteCore(pair.Value, func, new RewriteContext(ParentType.Object, pair.Key as string)));
                }
                return result;
            }
            return value;
        }

        private static Func<string, RewriteContext, string> GetRewriteFunc(RelativePath from, RelativePath to)
        {
            var funcs = Array.ConvertAll(RegexAndGroupIndexCollection, t => GetRewriteFuncCore(t.Item1, t.Item2, from, to));
            return (s, c) =>
            {
                if (c.Type == ParentType.List)
                {
                    return s;
                }
                if (c.Type == ParentType.Object)
                {
                    switch (c.ObjectPropertyName)
                    {
                        case "id":
                        case "uid":
                            return s;
                        default:
                            break;
                    }
                }
                for (int i = 0; i < funcs.Length; i++)
                {
                    s = funcs[i](s);
                }
                return s;
            };
        }

        private static Func<string, string> GetRewriteFuncCore(Regex regex, int groupIndex, RelativePath from, RelativePath to) =>
            s => regex.Replace(s, m => ReplaceRelativePath(m, m.Groups[groupIndex], from, to));

        private static string ReplaceRelativePath(Match m, Group g, RelativePath from, RelativePath to)
        {
            var path = g.Value;
            Uri uri;
            if (Uri.TryCreate(path, UriKind.Relative, out uri) &&
                !Path.IsPathRooted(path))
            {
                var rp = (RelativePath)path;
                rp = rp.Rebase(from, to);
                return m.Value.Remove(g.Index - m.Index) + rp.ToString() + m.Value.Substring(g.Index - m.Index + g.Length);
            }
            return m.Value;
        }

        private struct RewriteContext
        {
            public RewriteContext(ParentType type, string objectPropertyName)
            {
                Type = type;
                ObjectPropertyName = objectPropertyName;
            }

            public readonly ParentType Type;

            public readonly string ObjectPropertyName;
        }

        private enum ParentType
        {
            NoParent,
            Object,
            List,
        }
    }
}
