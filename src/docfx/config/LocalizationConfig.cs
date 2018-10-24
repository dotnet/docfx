// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal sealed class LocalizationConfig
    {
        public const string DefaultLocaleStr = "en-us";

        /// <summary>
        /// Gets the default locale of this docset.
        /// </summary>
        public readonly string DefaultLocale = DefaultLocaleStr;

        /// <summary>
        /// The mapping between source files and localized files
        /// </summary>
        public readonly LocalizationMapping Mapping;

        /// <summary>
        /// Show bilingual for localization pages
        /// </summary>
        public readonly bool Bilingual;
    }
}
