// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal interface ISchemaDocumentProvider
    {
        (Error[] errors, IXrefSpec[] xrefSpecs) LoadXrefSpecs(FilePath file);

        (Error[] errors, JToken model) LoadSchemaDocument(FilePath file);
    }
}
