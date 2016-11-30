// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System.Collections.Generic;
    using System.IO;

    public interface IFileAbstractLayer
    {
        bool CanWrite { get; }

        IEnumerable<RelativePath> GetAllInputFiles();

        IEnumerable<RelativePath> GetAllOutputFiles();

        bool Exists(RelativePath file);

        FileStream OpenRead(RelativePath file);

        FileStream Create(RelativePath file);

        void Copy(RelativePath sourceFileName, RelativePath destFileName);
    }
}
