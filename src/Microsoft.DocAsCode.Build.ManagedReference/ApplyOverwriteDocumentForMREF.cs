// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Plugins;

    using YamlDotNet.Core;

    [Export(nameof(ManagedReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class ApplyOverwriteDocumentForMref : ApplyOverwriteDocument
    {
        private readonly IModelAttributeHandler _handler =
            new CompositeModelAttributeHandler(
                new UniqueIdentityReferenceHandler(),
                new MarkdownContentHandler()
                );

        public override string Name => nameof(ApplyOverwriteDocumentForMref);

        public override int BuildOrder => 0x10;

        public IEnumerable<ItemViewModel> GetItemsFromOverwriteDocument(FileModel fileModel, string uid, IHostService host)
        {
            return Transform<ItemViewModel>(
                fileModel,
                uid,
                host);
        }

        public IEnumerable<ItemViewModel> GetItemsToOverwrite(FileModel fileModel, string uid, IHostService host)
        {
            return ((PageViewModel)fileModel.Content).Items.Where(s => s.Uid == uid);
        }

        protected override void ApplyOverwrite(IHostService host, List<FileModel> od, string uid, List<FileModel> articles)
        {
            ApplyOverwrite(host, od, uid, articles, GetItemsFromOverwriteDocument, GetItemsToOverwrite);
        }

        /// <summary>
        /// TODO: Move to base and share with other overwrite components
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="uid"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        private IEnumerable<T> Transform<T>(FileModel model, string uid, IHostService host) where T : class, IOverwriteDocumentViewModel
        {
            var overwrites = ((List<OverwriteDocumentModel>)model.Content).Where(s => s.Uid == uid);
            return overwrites.Select(s =>
            {
                try
                {
                    var placeholderContent = s.Conceptual;
                    s.Conceptual = null;
                    var item = s.ConvertTo<T>();
                    var context = new HandleModelAttributesContext
                    {
                        EnableContentPlaceholder = true,
                        Host = host,
                        PlaceholderContent = placeholderContent,
                        FileAndType = model.FileAndType,
                    };

                    _handler.Handle(item, context);
                    if (!context.ContainsPlaceholder)
                    {
                        item.Conceptual = placeholderContent;
                    }
                    return item;
                }
                catch (YamlException ye)
                {
                    throw new DocumentException($"Unable to deserialize YAML header from \"{s.Documentation.Path}\" Line {s.Documentation.StartLine} to TYPE {typeof(T).Name}: {ye.Message}", ye);
                }
            });
        }
    }
}
