// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Plugins;

    public abstract class BaseDocumentBuildStep : IDocumentBuildStep
    {
        public abstract string Name { get; }

        public abstract int BuildOrder { get; }

        public virtual IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
        {
            return models;
        }

        public virtual void Build(FileModel model, IHostService host)
        {
        }

        public virtual void Postbuild(ImmutableList<FileModel> models, IHostService host)
        {
        }

        /// <summary>
        /// TODO: merge with the one in BuildManagedReferenceDocument
        /// </summary>
        /// <param name="host"></param>
        /// <param name="markdown"></param>
        /// <param name="model"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static string Markup(IHostService host, string markdown, FileModel model, Func<string, bool> filter = null)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return markdown;
            }

            if (filter != null && filter(markdown))
            {
                return markdown;
            }

            var mr = host.Markup(markdown, model.FileAndType);
            ((HashSet<string>)model.Properties.LinkToFiles).UnionWith(mr.LinkToFiles);
            ((HashSet<string>)model.Properties.LinkToUids).UnionWith(mr.LinkToUids);
            return mr.Html;
        }
    }
}
