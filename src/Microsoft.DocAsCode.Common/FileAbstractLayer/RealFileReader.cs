
namespace Microsoft.DocAsCode.Common
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class RealFileReader : IFileReader
    {

        public RealFileReader(string inputFolder)
        {
            InputFolder = inputFolder;
        }

        public string InputFolder { get; }

        #region IFileReader Members

        public PathMapping? FindFile(RelativePath file)
        {
            var pp = Path.Combine(InputFolder, file.RemoveWorkingFolder());
            if (!File.Exists(pp))
            {
                return null;
            }
            return new PathMapping(file, pp);
        }

        public IEnumerable<RelativePath> EnumerateFiles()
        {
            var length = Path.GetFullPath(InputFolder).Length;
            return from f in Directory.EnumerateFiles(InputFolder, "*.*", SearchOption.AllDirectories)
                   select (RelativePath)f.Substring(length);
        }

        #endregion

    }
}
