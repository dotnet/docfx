// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    using Microsoft.DocAsCode.EntityModel.Builders;
    using Microsoft.DocAsCode.Plugins;

    [Trait("Owner", "zhyan")]
    [Trait("EntityType", "DocumentBuilder")]
    public class DocumentBuilderTest
    {
        [Fact]
        public void TestRelativePathRewriter()
        {
            var outputBaseDir = Path.Combine(Environment.CurrentDirectory, "output");
            var resourceFile = Path.GetFileName(typeof(DocumentBuilderTest).Assembly.Location);
            FileCollection files = new FileCollection(Environment.CurrentDirectory);
            files.Add(DocumentType.Resource, new[] { resourceFile });
            var builder = new DocumentBuilder();
            builder.Build(files, outputBaseDir);
            Assert.True(File.Exists(Path.Combine(outputBaseDir, resourceFile)));
        }
    }
}
