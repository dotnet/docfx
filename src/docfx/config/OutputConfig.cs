// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Microsoft.Docs.Build
{
    internal sealed class OutputConfig
    {
        /// <summary>
        /// Gets the build output directory. Could be absolute or relative.
        /// </summary>
        public string Path { get; } = "_site";

        /// <summary>
        /// Gets whether to output JSON model.
        /// </summary>
        public bool Json { get; private set; } = false;

        /// <summary>
        /// For backward compatibility.
        /// Gets whether to generate `_op_pdfUrlPrefixTemplate` property in legacy metadata conversion.
        /// Front-end will display `Download PDF` link if `_op_pdfUrlPrefixTemplate` property is set.
        /// </summary>
        public bool Pdf { get; private set; } = false;

        /// <summary>
        /// Gets whether to use ugly url or pretty url when <see cref="Json"/> is set to false.
        ///  - Pretty url:      a.md --> a/index.html
        ///  - Ugly url:        a.md --> a.html
        /// </summary>
        public bool UglifyUrl { get; private set; } = false;

        /// <summary>
        /// Gets whether to lowercase all URLs and output file path.
        /// </summary>
        public bool LowerCaseUrl { get; private set; } = true;

        /// <summary>
        /// Gets whether resources are copied to output.
        /// </summary>
        public bool CopyResources { get; private set; } = false;

        /// <summary>
        /// Gets the maximum errors to output.
        /// </summary>
        public int MaxErrors { get; private set; } = 1000;

        /// <summary>
        /// Gets the maximum warnings to output.
        /// </summary>
        public int MaxWarnings { get; private set; } = 1000;

        /// <summary>
        /// Gets the maximum suggestions to output.
        /// There are may be too many suggestion messages so increase the limit.
        /// </summary>
        public int MaxSuggestions { get; private set; } = 10000;
    }
}
