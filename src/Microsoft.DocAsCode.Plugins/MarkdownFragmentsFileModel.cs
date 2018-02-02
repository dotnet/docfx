// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Runtime.Serialization;

    public class MarkdownFragmentsFileModel
    {
        public FileAndType OriginalFileAndType { get; private set; }

        public FileAndType FileAndType { get; private set; }

        public ModelWithCache ModelWithCache { get; }

        public MarkdownFragmentsFileModel(FileAndType ft, object content, FileAndType original = null, IFormatter serializer = null)
        {
            OriginalFileAndType = original ?? ft;
            FileAndType = ft;
            ModelWithCache = new ModelWithCache(content, serializer);
        }
    }
}
