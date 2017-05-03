// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.PdfConverter
{
    public class PdfOptions
    {
        public string DestDirectory { get; set; }

        public string PdfDocsetName { get; set; }

        public string CssFilePath { get; set; }

        public bool GenerateAppendices { get; set; } = false;
    }
}
