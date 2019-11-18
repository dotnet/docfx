// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf
{
    public class PartialPdfModel
    {
        public string FilePath { get; set; }

        public int NumberOfPages { get; set; }

        public int? PageNumber { get; set; }
    }
}
