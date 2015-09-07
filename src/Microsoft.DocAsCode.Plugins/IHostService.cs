// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;

    public interface IHostService
    {
        string Markup(string markdown, FileAndType ft);
        ISet<string> GetAllUids();
        IEnumerable<FileModel> GetAllModels(DocumentType? type = null);
        FileAndType[] LookupByUid(string uid);
        FileModel GetModel(FileAndType uid);
    }
}
