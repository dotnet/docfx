// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.ExternalPackageGenerators.Msdn
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    internal sealed class ClassEntry
    {
        public ClassEntry(string entryName, List<CommentIdAndUid> items)
        {
            EntryName = entryName;
            Items = items;
        }

        public string EntryName { get; private set; }

        public List<CommentIdAndUid> Items { get; private set; }
    }

    internal sealed class CommentIdAndUid
    {
        public CommentIdAndUid(string commentId, string uid)
        {
            CommentId = commentId;
            Uid = uid;
        }

        public string CommentId { get; private set; }

        public string Uid { get; private set; }
    }

    internal sealed class EntryNameAndViewModel
    {
        public EntryNameAndViewModel(string entryName, List<XRefSpec> viewModel)
        {
            EntryName = entryName;
            ViewModel = viewModel;
        }

        public string EntryName { get; private set; }

        public List<XRefSpec> ViewModel { get; private set; }
    }
}
