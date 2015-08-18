namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class RelativePathRewriter
    {
        private static readonly Tuple<Regex, int>[] RegexAndGroupIndexCollection =
            new[]
            {
                Tuple.Create(new Regex(@"\[.+\]\((.+?)(\s+\""(.+?)\"")?\)", RegexOptions.Compiled), 1),
                Tuple.Create(new Regex(@"href=\""(.+?)\""", RegexOptions.Compiled | RegexOptions.IgnoreCase), 1),
                Tuple.Create(new Regex(@"src=\""(.+?)\""", RegexOptions.Compiled | RegexOptions.IgnoreCase), 1),
            };

        public static object Rewrite(object value, RelativePath from, RelativePath to)
        {
            return RewriteCore(
                value,
                GetRewriteFunc(from, to));
        }

        private static object RewriteCore(object value, Func<string, string> func)
        {
            var str = value as string;
            if (str != null)
            {
                return func(str);
            }
            var list = value as List<object>;
            if (list != null)
            {
                var result = new List<object>();
                foreach (var item in list)
                {
                    result.Add(RewriteCore(item, func));
                }
                return result;
            }
            var dict = value as Dictionary<object, object>;
            if (dict != null)
            {
                var result = new Dictionary<object, object>();
                foreach (var pair in dict)
                {
                    result.Add(pair.Key, RewriteCore(pair.Value, func));
                }
                return result;
            }
            return value;
        }

        private static Func<string, string> GetRewriteFunc(RelativePath from, RelativePath to)
        {
            var funcs = Array.ConvertAll(RegexAndGroupIndexCollection, t => GetRewriteFuncCore(t.Item1, t.Item2, from, to));
            return s =>
            {
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
    }
}
