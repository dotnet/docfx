// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class NewTest
    {
        public static TheoryData<string> TemplateTypes { get; } = new TheoryData<string>();

        static NewTest()
        {
            foreach (var templateType in Directory.GetDirectories(Path.Combine(AppContext.BaseDirectory, "data", "new")))
            {
                TemplateTypes.Add(templateType);
            }
        }

        [Theory]
        [MemberData(nameof(TemplateTypes))]
        public static void Create_Build_Docset(string type)
        {
            var path = Path.Combine("new-test", Guid.NewGuid().ToString("N"));

            Assert.Equal(0, Docfx.Run(new[] { "new", type, "-o", path }));
            Assert.Equal(0, Docfx.Run(new[] { "build", path }));
        }
    }
}
