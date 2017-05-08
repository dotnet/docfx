// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf
{
    using System.Collections.Generic;

    public class HtmlModel
    {
        public string Title { get; set; }

        public string HtmlFilePath { get; set; }

        public string ExternalLink { get; set; }

        public IList<HtmlModel> Children { get; set; }
    }
}