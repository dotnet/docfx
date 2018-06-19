// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal static class ConfigExtensions
    {
        public static (string depotName, string docsetName) SplitName(this Config config)
        {
            var nameParts = config.Name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length < 2)
            {
                return (string.Empty, config.Name);
            }

            return (config.Name, nameParts[1]);
        }
    }
}
