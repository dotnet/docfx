// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal sealed class LocalizationConfig
    {
        /// <summary>
        /// Gets the default locale of this docset.
        /// </summary>
        public readonly string DefaultLocale = "en-us";

        /// <summary>
        /// Show bilingual for localization pages
        /// </summary>
        public readonly bool Bilingual;
    }
}
