using System.Collections.Generic;
using System.Linq;

namespace ECMA2Yaml
{
    public static class LINQExtensions
    {
        public static void AddWithKey(this Dictionary<string, List<string>> dict, string key, string val)
        {
            if (!dict.ContainsKey(key))
            {
                dict[key] = new List<string>();
            }
            dict[key].Add(val);
        }

        public static List<string> GetOrDefault(this Dictionary<string, List<string>> dict, string key)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            return null;
        }

        public static IEnumerable<T> NullIfEmpty<T>(this IEnumerable<T> list)
        {
            if (list == null || !list.Any())
            {
                return null;
            }
            return list;
        }

        public static HashSet<T> NullIfEmpty<T>(this HashSet<T> list)
        {
            if (list == null || !list.Any())
            {
                return null;
            }
            return list;
        }

        public static List<T> MergeWith<T>(this List<T> left, List<T> right)
        {
            if (left == null)
            {
                return right;
            }
            if (right != null)
            {
                left.AddRange(right);
            }
            return left;
        }

        public static List<T> ConcatList<T>(this List<T> left, List<T> right)
        {
            if (left == null)
            {
                return right;
            }
            if (right != null)
            {
                return left.Concat(right).ToList();
            }
            return left;
        }

        /// <summary>
        /// we only need this for netstandard 2.0, so make it internal.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        internal static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            if (source == null)
            {
                return null;
            }
            return new HashSet<T>(source);
        }
    }
}
