// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class BuildResource
    {
        internal static (List<Error> errors, ResourceModel model, List<string> monikers)
            Build(Document file, MetadataProvider metadataProvider, MonikerProvider monikerProvider)
        {
            Debug.Assert(file.ContentType == ContentType.Resource);

            var errors = new List<Error>();
            var (metaErrors, metadata) = JsonUtility.ToObjectWithSchemaValidation<FileMetadata>(metadataProvider.GetMetadata(file));
            errors.AddRange(metaErrors);

            var (monikerError, monikers) = monikerProvider.GetFileLevelMonikers(file, metadata.MonikerRange);
            errors.AddIfNotNull(monikerError);

            return (errors, new ResourceModel
            {
                Locale = file.Docset.Locale,
                Monikers = monikers,
            }, monikers);
        }
    }
}
