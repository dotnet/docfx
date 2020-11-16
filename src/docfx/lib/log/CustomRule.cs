// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    [JsonConverter(typeof(ShortHandConverter))]
    internal class CustomRule
    {
        public ErrorLevel? Severity { get; private set; }

        public string? Code { get; private set; }

        public string? AdditionalMessage { get; private set; }

        public string? Message { get; private set; }

        public string? PropertyPath { get; private set; }

        public bool CanonicalVersionOnly { get; private set; }

        public bool PullRequestOnly { get; private set; }

        [JsonConverter(typeof(OneOrManyConverter))]
        public string[] Exclude { get; private set; } = Array.Empty<string>();

        [JsonConverter(typeof(OneOrManyConverter))]
        public string[]? ContentTypes { get; private set; }

        public bool Disabled { get; set; }

        private Func<string, bool>? _globMatcherCache;

        public CustomRule() { }

        public CustomRule(ErrorLevel? severity) => Severity = severity;

        public CustomRule(
            ErrorLevel? severity,
            string? code,
            string? message,
            string? additionalMessage,
            string? propertyPath,
            bool canonicalVersionOnly,
            bool pullRequestOnly,
            string[]? contentTypes,
            bool disabled)
        {
            Severity = severity;
            Code = code;

            Message = message;
            AdditionalMessage = additionalMessage;
            PropertyPath = propertyPath;
            CanonicalVersionOnly = canonicalVersionOnly;
            PullRequestOnly = pullRequestOnly;
            ContentTypes = contentTypes;
            Disabled = disabled;
        }

        public bool ExcludeMatches(string file)
        {
            var match = LazyInitializer.EnsureInitialized(ref _globMatcherCache, () => GlobUtility.CreateGlobMatcher(Exclude));

            return match(file);
        }
    }
}
