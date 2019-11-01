// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Microsoft.Docs.Build
{
    internal class DocumentProvider
    {
        private readonly Docset _docset;
        private readonly Docset _fallbackDocset;
        private readonly Input _input;
        private readonly TemplateEngine _templateEngine;
        private readonly ConcurrentDictionary<FilePath, Document> _documents = new ConcurrentDictionary<FilePath, Document>();

        public DocumentProvider(Docset docset, Docset fallbackDocset, Input input, TemplateEngine templateEngine)
        {
            _docset = docset;
            _fallbackDocset = fallbackDocset;
            _input = input;
            _templateEngine = templateEngine;
        }

        public Document GetDocument(FilePath path)
        {
            return _documents.GetOrAdd(path, GetDocumentCore);
        }

        private Document GetDocumentCore(FilePath path)
        {
            switch (path.Origin)
            {
                case FileOrigin.Fallback:
                    return Document.Create(_fallbackDocset, path, _input, _templateEngine);

                default:
                    return Document.Create(_docset, path, _input, _templateEngine);
            }
        }
    }
}
