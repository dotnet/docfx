// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using System.Linq;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Build.Engine.Incrementals.Outputs;
    using Microsoft.DocAsCode.Plugins;

    using Xunit;

    [Trait("Owner", "xuzho")]
    public class ExpandedDependencyMapTest
    {
        [Fact]
        public void BasicTest()
        {
            // A.md -> B.md include
            // B.md -> C.md file
            // B.md -> C.md bookmark
            // D.md -> B.md file
            var dg = new DependencyGraph();
            dg.ReportDependency(new[] {
                new DependencyItem("~/A.md", "~/B.md", "~/A.md", DependencyTypeName.Include),
                new DependencyItem("~/B.md", "~/C.md", "~/B.md", DependencyTypeName.File),
                new DependencyItem("~/B.md", "~/C.md", "~/B.md", DependencyTypeName.Bookmark),
                new DependencyItem("~/D.md", "~/B.md", "~/D.md", DependencyTypeName.File),
            });
            var edg = ExpandedDependencyMap.ConstructFromDependencyGraph(dg);
            Assert.Equal(
                new[]
                {
                    new ExpandedDependencyItem("~/A.md", "~/B.md", DependencyTypeName.Include),
                    new ExpandedDependencyItem("~/A.md", "~/C.md", DependencyTypeName.Bookmark),
                    new ExpandedDependencyItem("~/A.md", "~/C.md", DependencyTypeName.File),
                },
                edg.GetDependencyFrom("~/A.md").OrderBy(i => i.ToString()));
            Assert.Equal(
                new[]
                {
                    new ExpandedDependencyItem("~/B.md", "~/C.md", DependencyTypeName.Bookmark),
                    new ExpandedDependencyItem("~/B.md", "~/C.md", DependencyTypeName.File),
                },
                edg.GetDependencyFrom("~/B.md").OrderBy(i => i.ToString()));
            Assert.Equal(
                Enumerable.Empty<ExpandedDependencyItem>(),
                edg.GetDependencyFrom("~/C.md").OrderBy(i => i.ToString()));
            Assert.Equal(
                new[]
                {
                    new ExpandedDependencyItem("~/D.md", "~/B.md", DependencyTypeName.File),
                },
                edg.GetDependencyFrom("~/D.md").OrderBy(i => i.ToString()));
            Assert.Equal(
                Enumerable.Empty<ExpandedDependencyItem>(),
                edg.GetDependencyTo("~/A.md").OrderBy(i => i.ToString()));
            Assert.Equal(
                new[]
                {
                    new ExpandedDependencyItem("~/A.md", "~/B.md", DependencyTypeName.Include),
                    new ExpandedDependencyItem("~/D.md", "~/B.md", DependencyTypeName.File),
                },
                edg.GetDependencyTo("~/B.md").OrderBy(i => i.ToString()));
            Assert.Equal(
                new[]
                {
                    new ExpandedDependencyItem("~/A.md", "~/C.md", DependencyTypeName.Bookmark),
                    new ExpandedDependencyItem("~/A.md", "~/C.md", DependencyTypeName.File),
                    new ExpandedDependencyItem("~/B.md", "~/C.md", DependencyTypeName.Bookmark),
                    new ExpandedDependencyItem("~/B.md", "~/C.md", DependencyTypeName.File),
                },
                edg.GetDependencyTo("~/C.md").OrderBy(i => i.ToString()));
            Assert.Equal(
                Enumerable.Empty<ExpandedDependencyItem>(),
                edg.GetDependencyTo("~/D.md").OrderBy(i => i.ToString()));
        }
    }
}
