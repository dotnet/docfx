// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    public sealed class FileModel
    {
        public FileModel(FileAndType ft, object content)
        {
            OriginalFileAndType = ft;
            FileAndType = ft;
            Content = content;
        }

        public FileAndType FileAndType { get; private set; }

        public FileAndType OriginalFileAndType { get; private set; }

        public object Content { get; private set; }

        public string BaseDir
        {
            get
            {
                return FileAndType.BaseDir;
            }
            set
            {
                FileAndType = new FileAndType(value, File, Type);
            }
        }

        public string File
        {
            get
            {
                return FileAndType.File;
            }
            set
            {
                FileAndType = new FileAndType(BaseDir, value, Type);
            }
        }

        public DocumentType Type => FileAndType.Type;

        public string[] Uids { get; set; }
    }
}
