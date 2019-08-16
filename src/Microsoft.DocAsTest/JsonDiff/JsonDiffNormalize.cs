// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.DocAsTest
{
    /// <summary>
    /// Normalizes the expected <see cref="JToken"/> and actual <see cref="JToken"/>
    /// before performing a text diff.
    /// </summary>
    /// <param name="expected">The expected object.</param>
    /// <param name="actual">The actual object.</param>
    /// <param name="name">
    /// If the expected token and actual token is a property of a <see cref="JObject"/>,
    /// it is the name of that property, otherwise it is an empty string</param>
    /// <param name="diff">A <see cref="JsonDiff"/> instance with preconfigured rules.</param>
    /// <returns>
    /// A normalized expected token and an actual token, used for text diff.
    /// </returns>
    public delegate (JToken expected, JToken actual) JsonDiffNormalize(
        JToken expected, JToken actual, string name, JsonDiff diff);
}
