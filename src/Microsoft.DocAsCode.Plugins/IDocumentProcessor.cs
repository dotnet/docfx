// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections.Generic;

    public interface IDocumentProcessor
    {
        Type ArticleModelType { get; }
        FileModel Load(FileAndType file);
        void Save(FileModel model);
        IEnumerable<FileModel> Prebuild(IEnumerable<FileModel> models, IHostService host);
        FileModel BuildArticle(FileModel model, IHostService host);
        IEnumerable<FileModel> Postbuild(IEnumerable<FileModel> models, IHostService host);
    }
}
