// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.IO;

    using Xunit;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;

    [Trait("Owner", "lianwei")]
    public class TocRestructureTest : TestBase
    {
        [Fact]
        public void TestTocRestructureWithLeafNode()
        {
            var toc = GetTocItem(@"
root
    node1
        leaf1
        node2
            leaf2
            leaf3
    leaf3
");
            var restructures = new List<TreeItemRestructure>
            {
                GetRestructure(TreeItemActionType.AppendChild, "leaf2", new string[] { "leaf2.1", "leaf2.2" }),
                GetRestructure(TreeItemActionType.PrependChild, "leaf2", new string[] {"leaf2.3", "leaf2.4" }),
                GetRestructure(TreeItemActionType.InsertAfter, "leaf2", new string[] { "leaf4", "leaf5" }),
                GetRestructure(TreeItemActionType.InsertBefore, "leaf2", new string[] { "node2", "node3" }),
                GetRestructure(TreeItemActionType.DeleteSelf, "leaf3", new string[] { "leaf6", "leaf7" }),
                GetRestructure(TreeItemActionType.ReplaceSelf, "leaf3", new string[] { "leaf6" }),
            };
            TocRestructureUtility.Restructure(toc, restructures);
            var expected = GetTocItem(@"
root
    node1
        leaf1
        node2
            node2
            node3
            leaf2
                leaf2.3
                leaf2.4
                leaf2.1
                leaf2.2
            leaf4
            leaf5
");
            AssertTocEqual(expected, toc);
        }

        [Fact]
        public void TestTocRestructureWithContainerNode()
        {
            var toc = GetTocItem(@"
root
    node1
        leaf1
        node2
            leaf2
            leaf3
    node3
        leaf3.1
        leaf3.2
    node4
        node5
        leaf4.1
        leaf4.2
");
            var restructures = new List<TreeItemRestructure>
            {
                GetRestructure(TreeItemActionType.AppendChild, "node2", new string[] { "leaf2.1" }),
                GetRestructure(TreeItemActionType.AppendChild, "node2", new string[] { "leaf2.2" }),
                GetRestructure(TreeItemActionType.PrependChild, "node2", new string[] { "leaf2.3", "leaf2.4" }),
                GetRestructure(TreeItemActionType.InsertAfter, "node2", new string[] { "leaf4", "leaf5" }),
                GetRestructure(TreeItemActionType.InsertBefore, "node2", new string[] { "node2", "node3" }),
                GetRestructure(TreeItemActionType.DeleteSelf, "node3", null),
                GetRestructure(TreeItemActionType.ReplaceSelf, "node4", new string[] { "leaf6" }),
            };
            TocRestructureUtility.Restructure(toc, restructures);
            var expected = GetTocItem(@"
root
    node1
        leaf1
        node2
        node3
        node2
            leaf2.3
            leaf2.4
            leaf2
            leaf3
            leaf2.1
            leaf2.2
        leaf4
        leaf5
    leaf6
");
            AssertTocEqual(expected, toc);
        }

        [Fact]
        public void TestTocRestructureWithNoMatchNode()
        {
            var layout = @"
root
    node1
        leaf1
        node2
            leaf2
            leaf3
    leaf3
";
            var toc = GetTocItem(layout);
            var restructures = new List<TreeItemRestructure>
            {
                GetRestructure(TreeItemActionType.AppendChild, "leaf100", new string[] {"leaf2.1", "leaf2.2" }),
                GetRestructure(TreeItemActionType.PrependChild, "leaf100", new string[] {"leaf2.1", "leaf2.2" }),
                GetRestructure(TreeItemActionType.InsertAfter, "leaf100", new string[] {"leaf4", "leaf5" }),
                GetRestructure(TreeItemActionType.InsertBefore, "leaf100", new string[] {"node2", "node3" }),
                GetRestructure(TreeItemActionType.DeleteSelf, "leaf100", new string[] {"leaf6", "leaf7" }),
                GetRestructure(TreeItemActionType.ReplaceSelf, "leaf100", new string[] {"leaf6", "leaf7" }),
            };
            TocRestructureUtility.Restructure(toc, restructures);
            var expected = GetTocItem(layout);
            AssertTocEqual(expected, toc);
        }

        [Fact]
        public void TestReplaceNodeWithMultipleNodesThrows()
        {
            var toc = GetTocItem(@"
root
    node1
        leaf1
        leaf2
    leaf3
    node2
        node1
");
            var restructures = new List<TreeItemRestructure>
            {
                GetRestructure(TreeItemActionType.ReplaceSelf, "node2", new string[] {"leaf4", "leaf5" }),
            };
            Assert.Throws<InvalidOperationException>(() => TocRestructureUtility.Restructure(toc, restructures));
        }

        [Fact]
        public void TestTocRestructureWithRestructureConflicts()
        {
            var toc = GetTocItem(@"
root
    node1
        leaf1
        leaf2
    leaf3
    node2
        node1
");
            var restructures = new List<TreeItemRestructure>
            {
                GetRestructure(TreeItemActionType.InsertAfter, "node2", new string[] {"leaf4", "leaf5" }),
                GetRestructure(TreeItemActionType.DeleteSelf, "node2", new string[] {"leaf6", "leaf7" }),
                GetRestructure(TreeItemActionType.ReplaceSelf, "node2", new string[] {"leaf8", "leaf9" }),
                GetRestructure(TreeItemActionType.InsertBefore, "node2", new string[] {"leaf10", "leaf11" }),
            };
            TocRestructureUtility.Restructure(toc, restructures);
            var expected = GetTocItem(@"
root
    node1
        leaf1
        leaf2
    leaf3
    leaf4
    leaf5
");
            AssertTocEqual(expected, toc);
        }

        [Fact]
        public void TestTocRestructureAppliesToAllMatchedNodes()
        {
            var toc = GetTocItem(@"
root
    node1
        leaf1
        leaf3
    leaf3
    node2
        node1
");
            var restructures = new List<TreeItemRestructure>
            {
                GetRestructure(TreeItemActionType.AppendChild, "node1", new string[] {"leaf3", "leaf4" }),
                GetRestructure(TreeItemActionType.InsertBefore, "leaf3", new string[] {"leaf3.1", "leaf3.2" }),
            };

            // After leaf3 is appended as child, leaf3.1 and leaf3.2 should insert before leaf3.
            TocRestructureUtility.Restructure(toc, restructures);
            var expected = GetTocItem(@"
root
    node1
        leaf1
        leaf3.1
        leaf3.2
        leaf3
        leaf3.1
        leaf3.2
        leaf3
        leaf4
    leaf3.1
    leaf3.2
    leaf3
    node2
        node1
            leaf3.1
            leaf3.2
            leaf3
            leaf4
");
            AssertTocEqual(expected, toc);
        }

        private static void AssertTocEqual(TocItemViewModel expected, TocItemViewModel actual)
        {
            Assert.Equal(expected.ToJsonString(), actual.ToJsonString());
        }

        private TreeItemRestructure GetRestructure(TreeItemActionType actionType, string uid, string[] childrenUids)
        {
            return new TreeItemRestructure
            {
                ActionType = actionType,
                Key = uid,
                TypeOfKey = TreeItemKeyType.TopicUid,
                RestructuredItems = childrenUids?.Select(s => GetTreeItem(s)).ToImmutableList(),
            };
        }

        private TreeItem GetTreeItem(string uid)
        {
            return new TreeItem
            {
                Metadata = new Dictionary<string, object>
                {
                    ["topicUid"] = uid
                }
            };
        }

        private TocItemViewModel GetTocItem(string layout)
        {
            var lines = GetLines(layout).ToList();
            if (lines.Count == 0)
            {
                return null;
            }
            var root = new TocItemViewModel
            {
                Items = new TocViewModel()
            };
            var stack = new Stack<Tuple<LineInfo, TocItemViewModel>>();
            stack.Push(Tuple.Create(new LineInfo
            {
                Prefix = -1
            }, root));
            foreach (var line in lines)
            {
                var item = new TocItemViewModel
                {
                    TopicUid = line.Name,
                };

                while (stack.Peek().Item1.Prefix >= line.Prefix)
                {
                    stack.Pop();
                }
                var parent = stack.Peek();
                if (parent.Item2.Items == null)
                {
                    parent.Item2.Items = new TocViewModel();
                }
                parent.Item2.Items.Add(item);

                stack.Push(Tuple.Create(line, item));
            }
            return root;
        }

        private IEnumerable<LineInfo> GetLines(string layout)
        {
            return layout.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(s => s.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => GetLineInfo(s));
        }

        private LineInfo GetLineInfo(string line)
        {
            var name = line.TrimStart();
            var prefixLength = line.Length - name.Length;
            return new LineInfo
            {
                Name = name,
                Prefix = prefixLength,
            };
        }

        private sealed class LineInfo
        {
            public string Name { get; set; }
            public int Prefix { get; set; }
        }
    }
}
