// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class Metadata
    {
        public static JObject GetFromConfig(Document file)
        {
            Debug.Assert(file != null);

            var config = file.Docset.Config;
            var result = config.GlobalMetadata;
            foreach (var fileMeta in config.FileMetadata.Where(c => c.Match(file.FilePath)))
            {
                result = JsonUtility.Merge(result, fileMeta.Value);
            }

            return result;
        }
    }
}
