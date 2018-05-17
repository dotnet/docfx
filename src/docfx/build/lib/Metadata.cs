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
            var fileMetadata =
                from item in config.FileMetadata
                where item.Match(file.FilePath)
                select item.Value;

            return JsonUtility.Merge(config.GlobalMetadata, fileMetadata);
        }
    }
}
