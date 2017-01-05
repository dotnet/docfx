// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Xunit;

    using Microsoft.DocAsCode.Plugins;

    [Trait("Owner", "lianwei")]
    public class TreeNavigatorTest
    {
        [Fact]
        public void NavigateSimpleTreeShouldSucceed()
        {
            var treeItem = new TreeItem
            {
                Items = new List<TreeItem>
                {
                     GenerateLeaf("leaf1"),
                     GenerateLeaf("leaf2"),
                },
                Metadata = GenerateName("root")
            };

            var nav = new TreeNavigator(treeItem);
            Assert.Equal("root", GetName(nav.Current));

            var flag = nav.MoveToFirstChild();
            Assert.True(flag);
            Assert.Equal("leaf1", GetName(nav.Current));

            flag = nav.MoveToFirstChild();
            Assert.False(flag);
            Assert.Equal("leaf1", GetName(nav.Current));

            flag = nav.MoveToParent();
            Assert.True(flag);
            Assert.Equal("root", GetName(nav.Current));

            flag = nav.MoveTo(s => GetName(s) == "leaf2");
            Assert.True(flag);
            Assert.Equal("leaf2", GetName(nav.Current));

            flag = nav.MoveTo(s => GetName(s) == "root");
            Assert.True(flag);
            Assert.Equal("root", GetName(nav.Current));

            flag = nav.AppendChild(new TreeItem { Metadata = GenerateName("leaf3") });
            flag = nav.AppendChild(new TreeItem { Metadata = GenerateName("leaf4") });
            Assert.True(flag);
            Assert.Equal(4, treeItem.Items.Count);
            Assert.Equal("leaf3", treeItem.Items[2].Metadata["name"]);
            Assert.Equal("leaf4", treeItem.Items[3].Metadata["name"]);

            flag = nav.RemoveChild(s => GetName(s) == "leaf2");
            Assert.True(flag);
            Assert.Equal(3, treeItem.Items.Count);
            Assert.Equal("leaf4", treeItem.Items[2].Metadata["name"]);
            Assert.Equal("root", GetName(nav.Current));
        }

        private TreeItem GenerateLeaf(string name)
        {
            return new TreeItem
            {
                Metadata = GenerateName(name)
            };
        }

        private Dictionary<string, object> GenerateName(string name)
        {
            return new Dictionary<string, object>
            {
                ["name"] = name,
            };
        }

        private string GetName(TreeItem item)
        {
            object name = null;
            if (item?.Metadata?.TryGetValue("name", out name) == true && name is string)
            {
                return (string)name;
            }

            return null;
        }
    }
}
