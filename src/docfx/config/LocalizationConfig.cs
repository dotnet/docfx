// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal class LocalizationConfig
    {
        /// <summary>
        /// The mapping between source files and localized files
        /// </summary>
        public readonly LocalizationMapping Mapping;

        /// <summary>
        /// Show bilingual for localization pages
        /// </summary>
        public readonly bool Bilingual;

        public LocalizationConfig(LocalizationMapping mapping = LocalizationMapping.Repository, bool enableBilingual = false)
        {
            Mapping = mapping;
            Bilingual = enableBilingual;
        }
    }
}
