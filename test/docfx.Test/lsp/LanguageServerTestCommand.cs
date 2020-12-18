// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    public class LanguageServerTestCommand
    {
        public Dictionary<string, string> OpenFiles { get; init; }

        public Dictionary<string, string> EditFiles { get; init; }

        public Dictionary<string, JToken> ExpectDiagnostics { get; init; }

        public bool ExpectNoNotification { get; init; }
    }
}
