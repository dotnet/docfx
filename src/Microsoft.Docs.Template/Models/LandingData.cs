// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    [PageSchema]
    public class LandingData : ClassWithExtensionData
    {
        public string Title { get; set; }

        public string TitleSuffix { get; set; }

        public JObject Metadata { get; set; }

        public LandingDataAbstract Abstract { get; set; }

        public LandingDataSection[] Sections { get; set; }

        public string DocumentType { get; set; }
    }

    public sealed class LandingDataAbstract : ClassWithExtensionData
    {
        [Markdown]
        public string Description { get; set; }

        public LandingDataAside Aside { get; set; }

        public LandingDataMenu Menu { get; set; }
    }

    public sealed class LandingDataAside : ClassWithExtensionData
    {
        public string Title { get; set; }

        public string Width { get; set; }

        [Href]
        public string Href { get; set; }

        public LandingDataImage Image { get; set; }
    }

    public sealed class LandingDataImage : ClassWithExtensionData
    {
        [Href]
        public string Href { get; set; }

        [Href]
        public string Src { get; set; }

        public string Alt { get; set; }

        public string Height { get; set; }

        public string Width { get; set; }

        public string Role { get; set; }
    }

    public sealed class LandingDataMenu : ClassWithExtensionData
    {
        public string Title { get; set; }

        public LandingDataMenuItem[] Items { get; set; }
    }

    public sealed class LandingDataMenuItem : ClassWithExtensionData
    {
        [Href]
        public string Href { get; set; }

        public string Text { get; set; }
    }

    public enum LandingDataType
    {
        Paragraph,
        List,
        Table,
        Markdown,
        Text,
    }

    public sealed class LandingDataSection : ClassWithExtensionData
    {
        public string Title { get; set; }

        public string Type { get; set; }

        public string Text { get; set; }

        public LandingDataItem[] Items { get; set; }
    }

    public sealed class LandingDataItem : ClassWithExtensionData
    {
        public LandingDataType Type { get; set; }

        [Markdown]
        public string Text { get; set; }

        public string Style { get; set; }

        public string ClassName { get; set; }

        public LandingDataListItem[] Items { get; set; }

        public JToken Columns { get; set; }

        public LandingDataRow[] Rows { get; set; }

        [Html]
        public string Html { get; set; }
    }

    public sealed class LandingDataColumn : ClassWithExtensionData
    {
        public LandingDataImage Image { get; set; }

        public string Title { get; set; }

        public string Text { get; set; }
    }

    public sealed class LandingDataRow : ClassWithExtensionData
    {
        public string Title { get; set; }

        public LandingDataRowValue[] Values { get; set; }
    }

    public sealed class LandingDataRowValue : ClassWithExtensionData
    {
        [Href]
        public string Href { get; set; }
    }

    public sealed class LandingDataListItem : ClassWithExtensionData
    {
        public string Text { get; set; }

        public string Title { get; set; }

        [Markdown]
        public string Content { get; set; }

        [Html]
        public string Html { get; set; }

        [Href]
        public string Href { get; set; }

        public LandingDataImage Image { get; set; }
    }

    public class ClassWithExtensionData
    {
        [JsonExtensionData(WriteData = false)]
        public JObject ExtensionData { get; set; }
    }
}
