// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class LandingData
    {
        public string Title { get; set; }

        public JObject Metadata { get; set; }

        public LandingDataAbstract Abstract { get; set; }

        public LandingDataSection[] Sections { get; set; }

        internal class LandingDataAbstract
        {
            public string Description { get; set; }
        }

        internal enum LandingDataType
        {
            Paragraph,
            List,
        }

        internal class LandingDataSection
        {
            public string Title { get; set; }

            public LandingDataItem[] Items { get; set; }
        }

        internal class LandingDataItem
        {
            public LandingDataType Type { get; set; }

            public string Text { get; set; }

            public string Style { get; set; }

            public string ClassName { get; set; }
        }

        internal class LandingDataListItem
        {
            public string Title { get; set; }

            public string Html { get; set; }
        }
    }
}
