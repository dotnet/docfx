using System.Linq;

namespace ECMA2Yaml
{
    public static class StringExtensions
    {
        /// <summary>Defaults to <see cref="F:System.IO.Path.DirectorySeparatorChar"/>.
        /// Should only be changed for unit testing purposes.</summary>
        public static char DirectorySeparatorChar = System.IO.Path.DirectorySeparatorChar;

        public static string NormalizePath(this string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            char otherSepChar = '/';

            if (DirectorySeparatorChar == '/')
                otherSepChar = '\\';

            if (path.Contains(otherSepChar))
                path = path.Replace(otherSepChar, DirectorySeparatorChar);

            return path;
        }

        public static string AppendDirectorySeparator(this string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            if (path.Last() == DirectorySeparatorChar) return path;

            return path + DirectorySeparatorChar;
        }

        public static int CountIndent(this string line)
        {
            var indent = 0;
            if (!string.IsNullOrEmpty(line))
            {
                while (indent < line.Length && char.IsWhiteSpace(line[indent]))
                {
                    indent++;
                }
            }

            return indent;
        }

        public static string TrimIndent(this string line, int indent)
        {
            if (string.IsNullOrEmpty(line) || line.Length < indent || !char.IsWhiteSpace(line[0]))
            {
                return line;
            }
            return line.Substring(indent);
        }
    }
}
