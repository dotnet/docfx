// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{

    public class TabItemBlock
    {
        public string Id { get; }

        public string Condition { get; }

        public TabTitleBlock Title { get; }

        public TabContentBlock Content { get; }

        public bool Visible { get; set; }

        public TabItemBlock(string id, string condition, TabTitleBlock title, TabContentBlock content, bool visible)
        {
            Id = id;
            Condition = condition;
            Title = title;
            Content = content;
            Visible = visible;
        }
    }
}