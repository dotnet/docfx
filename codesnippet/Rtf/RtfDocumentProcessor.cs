// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace RtfDocumentProcessors
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;

    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    [Export(typeof(IDocumentProcessor))]
    public class RtfDocumentProcessor : IDocumentProcessor
    {
        #region BuildSteps
        [ImportMany(nameof(RtfDocumentProcessor))]
        public IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }
        #endregion

        #region Name
        public string Name => nameof(RtfDocumentProcessor);
        #endregion

        #region GetProcessingPriority
        public ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            if (file.Type == DocumentType.Article &&
                ".rtf".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
            {
                return ProcessingPriority.Normal;
            }
            return ProcessingPriority.NotSupported;
        }
        #endregion

        #region Load
        public FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            var content = new Dictionary<string, object>
            {
                ["conceptual"] = File.ReadAllText(Path.Combine(file.BaseDir, file.File)),
                ["type"] = "Conceptual",
                ["path"] = file.File,
            };
            return new FileModel(file, content);
        }
        #endregion

        #region Save
        public SaveResult Save(FileModel model)
        {
            return new SaveResult
            {
                DocumentType = "Conceptual",
                ModelFile = model.File,
                LinkToFiles = linkToFiles.ToImmutableArray(),
            };
        }
        #endregion

        #region UpdateHref
        public void UpdateHref(FileModel model, IDocumentBuildContext context)
        {
        }
        #endregion
    }
}
