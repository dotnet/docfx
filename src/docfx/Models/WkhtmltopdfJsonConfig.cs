// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    ///     Holds configuration options specific to the wkhtmltopdf tooling used by the pdf command.
    /// </summary>
    [Serializable]
    public class WkhtmltopdfJsonConfig
    {
        /// <summary>
        /// Gets or sets the path and file name of a wkhtmltopdf.exe compatible executable.
        /// </summary>
        [JsonProperty("filePath")]
        public string FilePath { get; set; }

        /// <summary>
        /// Specify additional command line arguments that should be passed to the wkhtmltopdf executable.
        /// </summary>
        [JsonProperty("additionalArguments")]
        public string AdditionalArguments { get; set; }
    }
}