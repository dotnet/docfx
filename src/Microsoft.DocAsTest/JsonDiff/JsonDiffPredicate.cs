// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.DocAsTest
{
    /// <summary>
    /// Determines if the expected <see cref="JToken"/> and actual <see cref="JToken"/>
    /// matches a condition.
    /// </summary>
    /// <param name="expected">The expected object.</param>
    /// <param name="actual">The actual object.</param>
    /// <param name="name">
    /// If the expected token and actual token is a property of a <see cref="JObject"/>,
    /// it is the name of that property, otherwise it is an empty string</param>
    /// <returns>True if this predicate matches</returns>
    public delegate bool JsonDiffPredicate(JToken expected, JToken actual, string name);
}
