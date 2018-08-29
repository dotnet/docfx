// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    [PageSchema]
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

        public LandingDataAside Aside { get; set; }

        public LandingDataMenu Menu { get; set; }
    }

    public class LandingDataAside
    {
        public string Title { get; set; }

        [Href]
        public string Href { get; set; }

        public LandingDataImage Image { get; set; }
    }

    public class LandingDataImage
    {
        [Href]
        public string Href { get; set; }

        public string Src { get; set; }

        public string Alt { get; set; }

        public string Width { get; set; }

        public string Role { get; set; }
    }

    public class LandingDataMenu
    {
        public string Title { get; set; }
    }

    public enum LandingDataType
    {
        Paragraph,
        List,
        Table,
        Markdown,
    }

    public class LandingDataSection
    {
        public string Title { get; set; }

        public LandingDataItem[] Items { get; set; }
    }

    public class LandingDataItem
    {
        public LandingDataType Type { get; set; }

        [Markdown]
        public string Text { get; set; }

        public string Style { get; set; }

        public string ClassName { get; set; }

        public LandingDataListItem[] Items { get; set; }

        public LandingDataColumn[] Columns { get; set; }

        public LandingDataRow[] Rows { get; set; }
    }

    public class LandingDataColumn
    {
        public LandingDataImage Image { get; set; }

        public string Title { get; set; }

        public string Text { get; set; }
    }

    public class LandingDataRow
    {
        public string Title { get; set; }

        public LandingDataRowValue[] Values { get; set; }
    }

    public class LandingDataRowValue
    {
        [Href]
        public string Href { get; set; }
    }

    public class LandingDataListItem
    {
        public string Text { get; set; }

        public string Title { get; set; }

        [Markdown]
        public string Content { get; set; }

        public string Html { get; set; }

        [Href]
        public string Href { get; set; }

        public LandingDataImage Image { get; set; }
    }

}
