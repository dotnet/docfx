// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal sealed class LegacyConfig
    {
        /// <summary>
        /// For backward compatibility, the source path prefix
        /// </summary>
        public readonly string SourceBasePath = ".";

        /// <summary>
        /// For backward compatibility, the output site path prefix
        /// </summary>
        public readonly string SiteBasePath = ".";

        /// <summary>
        /// For backward compatibility, whether generate pdf url template in medadata
        /// </summary>
        public readonly bool NeedGeneratePdfUrlTemplate = false;
    }
}
