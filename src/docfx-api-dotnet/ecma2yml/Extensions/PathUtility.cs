using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ECMA2Yaml
{
    public static class PathUtility
    {
        private static readonly char[] AdditionalInvalidChars = ":*".ToArray();
        public static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars().Concat(AdditionalInvalidChars).ToArray();
        public static readonly char[] InvalidPathChars = Path.GetInvalidPathChars().Concat(AdditionalInvalidChars).ToArray();
        private static readonly string InvalidFileNameCharsRegexString = $"[{Regex.Escape(new string(InvalidFileNameChars))}]";
        private static readonly string NeedUrlEncodeFileNameCharsRegexString = "[^0-9a-zA-Z-_.!*()]";

        private static readonly string InvalidOrNeedUrlEncodeFileNameCharsRegexString = $"{InvalidFileNameCharsRegexString}|{NeedUrlEncodeFileNameCharsRegexString}";
        private static readonly Regex InvalidOrNeedUrlEncodeFileNameCharsRegex = new Regex(InvalidOrNeedUrlEncodeFileNameCharsRegexString, RegexOptions.Compiled);

        public static string ToCleanUrlFileName(this string input, string replacement = "-")
        {
            if (string.IsNullOrEmpty(input))
            {
                return Path.GetRandomFileName();
            }

            return InvalidOrNeedUrlEncodeFileNameCharsRegex.Replace(input, replacement);
        }
    }
}
