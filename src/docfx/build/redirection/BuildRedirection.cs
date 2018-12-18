// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal static class BuildRedirection
    {
        internal static (List<Error> errors, RedirectionModel model, List<string> monikers)
            Build(Document file, MetadataProvider metadataProvider, MonikerProvider monikerProvider)
        {
            Debug.Assert(file.ContentType == ContentType.Redirection);
            var errors = new List<Error>();

            var (metaErrors, metadata) = JsonUtility.ToObjectWithSchemaValidation<FileMetadata>(metadataProvider.GetMetadata(file, null));
            errors.AddRange(metaErrors);

            var (error, monikers) = monikerProvider.GetFileLevelMonikers(file, metadata.MonikerRange);
            errors.AddIfNotNull(error);

            return (errors, new RedirectionModel
            {
                RedirectUrl = file.RedirectionUrl,
                Locale = file.Docset.Locale,
                Monikers = monikers,
            }, monikers);
        }
    }
}
