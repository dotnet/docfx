namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System;

    public sealed class FileAndType
        : IEquatable<FileAndType>
    {
        public FileAndType(string baseDir, string file, DocumentType type)
        {
            BaseDir = baseDir;
            File = file;
            Type = type;
        }

        public string BaseDir { get; private set; }

        public string File { get; private set; }

        public DocumentType Type { get; private set; }

        public FileAndType ChangeType(DocumentType type)
        {
            return new FileAndType(BaseDir, File, type);
        }

        public bool Equals(FileAndType other)
        {
            if (other == null)
            {
                return false;
            }
            return File == other.File && Type == other.Type && BaseDir == other.BaseDir;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FileAndType);
        }

        public override int GetHashCode()
        {
            return File.GetHashCode() + (int)Type ^ BaseDir.GetHashCode();
        }
    }
}
