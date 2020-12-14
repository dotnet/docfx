// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    public class LanguageServerTestSpec
    {
        public string Notification { get; set; } = string.Empty;

        public string ExpectedNotification { get; set; } = string.Empty;

        public JToken Params { get; set; }
    }
}
