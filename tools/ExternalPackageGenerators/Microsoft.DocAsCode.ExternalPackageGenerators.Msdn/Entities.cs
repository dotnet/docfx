namespace Microsoft.DocAsCode.ExternalPackageGenerators.Msdn
{
    using EntityModel.ViewModels;
    using System.Collections.Generic;

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
        public EntryNameAndViewModel(string entryName, List<ReferenceViewModel> viewModel)
        {
            EntryName = entryName;
            ViewModel = viewModel;
        }

        public string EntryName { get; private set; }

        public List<ReferenceViewModel> ViewModel { get; private set; }
    }
}
