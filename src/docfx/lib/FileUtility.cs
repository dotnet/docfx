// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal static class FileUtility
    {
        public static T ReadJsonFile<T>(string path)
        {
            var content = File.ReadAllText(path);

            try
            {
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch (Exception ex)
            {
                throw Errors.BadFileFormat(path, ex).ToException(ex);
            }
        }
    }
}
