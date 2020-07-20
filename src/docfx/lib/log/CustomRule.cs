// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    [JsonConverter(typeof(ShortHandConverter))]
    internal class CustomRule
    {
        public ErrorLevel? Severity { get; private set; }

        public string? Code { get; private set; }

        public string? AdditionalMessage { get; private set; }

        public bool CanonicalVersionOnly { get; private set; }

        public bool PullRequestOnly { get; private set; }

        public CustomRule() { }

        public CustomRule(ErrorLevel? severity) => Severity = severity;

        public CustomRule(ErrorLevel? severity, string? code, string? additionalMessage, bool canonicalVersionOnly, bool pullRequestOnly)
        {
            Severity = severity;
            Code = code;
            AdditionalMessage = additionalMessage;
            CanonicalVersionOnly = canonicalVersionOnly;
            PullRequestOnly = pullRequestOnly;
        }
    }
}
