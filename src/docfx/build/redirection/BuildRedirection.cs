// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildRedirection
    {
        internal static (List<Error>, RedirectionModel) Build(Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Redirection);
            var errors = new List<Error>();

            var (metaErrors, metadata) = JsonUtility.ToObjectWithSchemaValidation<FileMetadata>(file.Docset.Metadata.GetMetadata(file, null));
            errors.AddRange(metaErrors);

            var (error, monikers) = file.Docset.Monikers.GetFileLevelMonikers(file, metadata.MonikerRange);
            errors.AddIfNotNull(error);

            return (errors, new RedirectionModel
            {
                RedirectUrl = file.RedirectionUrl,
                Locale = file.Docset.Locale,
                Monikers = monikers,
            });
        }
    }
}
