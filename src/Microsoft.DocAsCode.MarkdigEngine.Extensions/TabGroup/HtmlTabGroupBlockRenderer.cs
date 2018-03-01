// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Renderers;
    using Markdig.Renderers.Html;

    public class HtmlTabGroupBlockRenderer : HtmlObjectRenderer<TabGroupBlock>
    {
        protected override void Write(HtmlRenderer renderer, TabGroupBlock block)
        {
            renderer.Write(@"<div class=""tabGroup"" id=""tabgroup_");
            var groupId = ExtensionsHelper.Escape(block.Id);
            renderer.Write(groupId);
            renderer.Write("\"");
            renderer.WriteAttributes(block);
            renderer.Write(">\n");

            WriteTabHeaders(renderer, block, groupId);
            WriteTabSections(renderer, block, groupId);

            renderer.Write("</div>\n");
        }

        private void WriteTabHeaders(HtmlRenderer renderer, TabGroupBlock block, string groupId)
        {
            renderer.Write("<ul role=\"tablist\">\n");
            for (var i = 0; i < block.Items.Length; i++)
            {
                var item = block.Items[i];
                renderer.Write("<li role=\"presentation\"");
                if (!item.Visible)
                {
                    renderer.Write(" aria-hidden=\"true\" hidden=\"hidden\"");
                }
                renderer.Write(">\n");
                renderer.Write(@"<a href=""#tabpanel_");
                AppendGroupId(renderer, groupId, item);
                renderer.Write(@""" role=""tab"" aria-controls=""tabpanel_");
                AppendGroupId(renderer, groupId, item);
                renderer.Write(@""" data-tab=""");
                renderer.Write(item.Id);
                if (!string.IsNullOrEmpty(item.Condition))
                {
                    renderer.Write(@""" data-condition=""");
                    renderer.Write(item.Condition);
                }
                if (i == block.ActiveTabIndex)
                {
                    renderer.Write("\" tabindex=\"0\" aria-selected=\"true\"");
                }
                else
                {
                    renderer.Write("\" tabindex=\"-1\"");
                }
                renderer.WriteAttributes(item.Title);
                renderer.Write(">");
                renderer.Render(item.Title);
                renderer.Write("</a>\n");
                renderer.Write("</li>\n");
            }
            renderer.Write("</ul>\n");
        }

        private void WriteTabSections(HtmlRenderer renderer, TabGroupBlock block, string groupId)
        {
            for (var i = 0; i < block.Items.Length; i++)
            {
                var item = block.Items[i];
                renderer.Write(@"<section id=""tabpanel_");
                AppendGroupId(renderer, groupId, item);
                renderer.Write(@""" role=""tabpanel"" data-tab=""");
                renderer.Write(item.Id);

                if (!string.IsNullOrEmpty(item.Condition))
                {
                    renderer.Write(@""" data-condition=""");
                    renderer.Write(item.Condition);
                }

                if (i == block.ActiveTabIndex)
                {
                    renderer.Write("\">\n");
                }
                else
                {
                    renderer.Write("\" aria-hidden=\"true\" hidden=\"hidden\">\n");
                }
                renderer.Render(item.Content);
                renderer.Write("</section>\n");
            }
        }

        private void AppendGroupId(HtmlRenderer renderer, string groupId, TabItemBlock item)
        {
            renderer.Write(groupId);
            renderer.Write("_");
            renderer.Write(item.Id);
            if (!string.IsNullOrEmpty(item.Condition))
            {
                renderer.Write("_");
                renderer.Write(item.Condition);
            }
        }
    }
}
