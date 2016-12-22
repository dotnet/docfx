// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ReferenceBase
{
    using System;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Plugins;

    /// <summary>
    /// Base document build step for reference.
    /// </summary>
    public abstract class BuildReferenceDocumentBase : BaseDocumentBuildStep
    {
        public override int BuildOrder => 0;

        public override void Build(FileModel model, IHostService host)
        {
            switch (model.Type)
            {
                case DocumentType.Article:
                    BuildArticle(host, model);
                    break;
                case DocumentType.Overwrite:
                    BuildOverwrite(host, model);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        protected abstract void BuildArticle(IHostService host, FileModel model);

        protected abstract void BuildOverwrite(IHostService host, FileModel model);
    }
}
