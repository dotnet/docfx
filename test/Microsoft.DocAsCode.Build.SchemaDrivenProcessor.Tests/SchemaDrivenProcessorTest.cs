// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDrivenProcessor.Tests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Web;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;

    using Newtonsoft.Json.Linq;
    using Xunit;

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "SchemaDrivenProcessorTest")]
    public class SchemaDrivenProcessorTest : TestBase
    {
        public SchemaDrivenProcessorTest()
        {
        }
    }
}
