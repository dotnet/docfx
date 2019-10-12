// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal static class InputUtility
    {
        /// <summary>
        /// Finds a yaml or json file under the specified location
        /// </summary>
        public static FilePath FindYamlOrJson(this Input input, FileOrigin origin, string pathWithoutExtension)
        {
            var fullPath = PathUtility.NormalizeFile(pathWithoutExtension + ".yml");
            var filePath = new FilePath(fullPath, origin);
            if (input.Exists(filePath))
            {
                return filePath;
            }

            fullPath = PathUtility.NormalizeFile(pathWithoutExtension + ".json");
            filePath = new FilePath(fullPath, origin);
            if (input.Exists(filePath))
            {
                return filePath;
            }

            return null;
        }
    }
}
