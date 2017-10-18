// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Plugins;

    public class OverwriteDocumentProcessor : DisposableDocumentProcessor
    {
        public override string Name => nameof(OverwriteDocumentProcessor);

        public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; } = new IDocumentBuildStep[] { new BuildOverwriteDocument() };

        public override ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            if (file.Type == DocumentType.Overwrite)
            {
                return ProcessingPriority.High;
            }

            return ProcessingPriority.NotSupported;
        }

        public override FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            switch (file.Type)
            {
                case DocumentType.Overwrite:
                    return OverwriteDocumentReader.Read(file);
                default:
                    throw new NotSupportedException();
            }
        }

        public override SaveResult Save(FileModel model)
        {
            return null;
        }
    }
}
