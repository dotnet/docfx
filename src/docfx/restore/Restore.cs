// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Docs
{
    internal static class Restore
    {
        public static Task Run(string docsetPath, CommandLineOptions options, Context context)
        {
            // Restore has to use Config directly, it cannot depend on Docset,
            // because Docset assumes the repo to physically exist on disk.
            var config = Config.Load(docsetPath, options);

            throw new NotImplementedException();
        }
    }
}
