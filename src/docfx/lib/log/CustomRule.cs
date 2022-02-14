// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build;

[JsonConverter(typeof(ShortHandConverter))]
internal record CustomRule
{
    public ErrorLevel? Severity { get; init; }

    public string? Code { get; init; }

    public string? AdditionalMessage { get; init; }

    public string? Message { get; init; }

    public string? PropertyPath { get; init; }

    public bool CanonicalVersionOnly { get; init; }

    // TODO: Retire PullRequestOnly.
    public bool PullRequestOnly { get; init; }

    public bool AddOnly { get; init; }

    [JsonConverter(typeof(OneOrManyConverter))]
    public string[] Exclude { get; init; } = Array.Empty<string>();

    [JsonConverter(typeof(OneOrManyConverter))]
    public string[]? ContentTypes { get; init; }

    [JsonConverter(typeof(OneOrManyConverter))]
    public string[]? Tags { get; init; }

    public bool Disabled { get; init; }

    private Func<string, bool>? _globMatcherCache;

    public CustomRule() { }

    public CustomRule(ErrorLevel? severity) => Severity = severity;

    public bool ExcludeMatches(string file)
    {
        var match = LazyInitializer.EnsureInitialized(ref _globMatcherCache, () => GlobUtility.CreateGlobMatcher(Exclude));

        return match(file);
    }
}
