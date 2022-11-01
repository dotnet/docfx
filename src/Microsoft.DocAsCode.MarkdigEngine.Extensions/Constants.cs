// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    public static class Constants
    {
        public static readonly string FencedCodePrefix = "lang-";

        public static class WarningCodes
        {
            public const string InvalidTabGroup = "InvalidTabGroup";
        }

        /// <summary>
        /// Names of properties supported in the markdownEngineProperties
        /// property in the docfx.json
        /// </summary>
        public static class EngineProperties
        {
            /// <summary>
            /// Enables the <see cref="LineNumberExtension"/>.
            /// </summary>
            public const string EnableSourceInfo = "EnableSourceInfo";

            /// <summary>
            /// Contains a list of optional Markdig extensions that are not
            /// enabled by default by DocFX.
            /// </summary>
            public const string MarkdigExtensions = "markdigExtensions";
        }
    }
}
