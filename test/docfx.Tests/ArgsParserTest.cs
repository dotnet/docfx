// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.SubCommands;

    using Xunit;

    public class ArgsParserTest
    {
        [Theory]
        [Trait("Related", "docfx")]
        [InlineData(new string[] { "help" }, typeof(HelpCommand))]
        public void TestArgsParser(string[] args, Type expectedType)
        {
            var controller = ArgsParser.Instance.Parse(args);
            var command = controller.Create();
            Assert.Equal(expectedType, command.GetType());
        }
    }
}
