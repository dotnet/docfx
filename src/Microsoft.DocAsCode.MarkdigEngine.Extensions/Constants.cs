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
        /// Optional extensions that can be enabled from
        /// the markdownEngineProperties property in the docfx.json.
        /// </summary>
        public static class OptionalExtensionPropertyNames
        {
            /// <summary>
            /// Enables the Task List Markdig extension by invoking <see cref="Markdig.MarkdownExtensions.UseTaskLists(Markdig.MarkdownPipelineBuilder)"/>.
            /// </summary>
            public const string EnableTaskLists = "enableTaskLists";

            /// <summary>
            /// Enables the Grid Tables Markdig extension by invoking <see cref="Markdig.MarkdownExtensions.UseGridTables(Markdig.MarkdownPipelineBuilder)"/>.
            /// </summary>
            public const string EnableGridTables = "enableGridTables";

            /// <summary>
            /// Enables the Footnotes Markdig extension by invoking <see cref="Markdig.MarkdownExtensions.UseFootnotes(Markdig.MarkdownPipelineBuilder)"/>.
            /// </summary>
            public const string EnableFootnotes = "enableFootnotes";

            /// <summary>
            /// Enables the Mathematics Markdig extension by invoking <see cref="Markdig.MarkdownExtensions.UseMathematics(Markdig.MarkdownPipelineBuilder)"/>.
            /// </summary>
            public const string EnableMathematics = "enableMathematics";

            /// <summary>
            /// Enables the Diagrams Markdig extension by invoking <see cref="Markdig.MarkdownExtensions.UseDiagrams(Markdig.MarkdownPipelineBuilder)"/>.
            /// </summary>
            public const string EnableDiagrams = "enableDiagrams";

            /// <summary>
            /// Enables the Definition Lists Markdig extension by invoking <see cref="Markdig.MarkdownExtensions.UseDefinitionLists(Markdig.MarkdownPipelineBuilder)"/>.
            /// </summary>
            public const string EnableDefinitionLists = "enableDefinitionLists";
        }
    }
}
