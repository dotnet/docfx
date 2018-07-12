using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class LegacyUtility
    {
        public static void MoveFileSafe(string sourceFileName, string destFileName)
        {
            Debug.Assert(!string.IsNullOrEmpty(sourceFileName));
            Debug.Assert(!string.IsNullOrEmpty(destFileName));
            Debug.Assert(File.Exists(sourceFileName));

            Directory.CreateDirectory(Path.GetDirectoryName(destFileName));
            File.Delete(destFileName);
            File.Move(sourceFileName, destFileName);
        }
    }
}
