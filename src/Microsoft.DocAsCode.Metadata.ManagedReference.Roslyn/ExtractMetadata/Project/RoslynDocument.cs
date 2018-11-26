// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using Microsoft.CodeAnalysis;
    
    public class RoslynDocument : AbstractDocument
    {
        Document _document;

        public RoslynDocument(Document document)
        {
            _document = document;
        }

        public override string FilePath => _document.FilePath;
    }
}
