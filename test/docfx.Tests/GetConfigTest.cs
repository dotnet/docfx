// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Xunit;

    public class GetConfigTest
    {
        /// <summary>
        /// As similar to run docfx.exe directly: search for docfx.json in current directory
        /// </summary>
        [Fact]
        [Trait("Related", "docfx")]
        public void TestGetConfigWithNoInputAndDocfxJsonExists()
        {
            try
            {
                File.Delete(Constants.ConfigFileName);
                File.Copy("Assets/docfx.sample.1.json", Constants.ConfigFileName);
                Options options = new Options();
                var config = (CompositeCommand)CommandFactory.GetCommand(options);
                Assert.Equal(2, config.Commands.Count);
                Assert.Equal(2, ((MetadataCommand)config.Commands[0]).Config.Count);
            }
            finally
            {
                File.Delete(Constants.ConfigFileName);
            }
        }

        [Fact]
        [Trait("Related", "docfx")]
        public void TestGetConfigWithNoInputAndDocfxJsonNotExists()
        {
            Assert.Throws(typeof(FileNotFoundException), () => CommandFactory.GetCommand(new Options()));
        }
    }
}
