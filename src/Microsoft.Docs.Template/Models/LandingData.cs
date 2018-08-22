// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    [DataSchema]
    public class LandingData
    {
        public string Title { get; set; }

        public JObject Metadata { get; set; }

        public LandingDataAbstract Abstract { get; set; }

        public LandingDataSection[] Sections { get; set; }

        public string DocumentType { get; set; }
    }

    public class LandingDataAbstract
    {
        public string Description { get; set; }
    }

    public enum LandingDataType
    {
        Paragraph,
        List,
        Table,
        Markdown,
        Text,
    }

    public class LandingDataSection
    {
        public string Title { get; set; }

        public LandingDataItem[] Items { get; set; }
    }

    public class LandingDataItem
    {
        public LandingDataType Type { get; set; }

        public string Text { get; set; }

        public string Style { get; set; }

        public string ClassName { get; set; }
    }

    public class LandingDataListItem
    {
        public string Title { get; set; }

        public string Html { get; set; }
    }
}
