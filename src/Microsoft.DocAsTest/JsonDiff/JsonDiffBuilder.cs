// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DocAsTest
{
    public class JsonDiffBuilder
    {
        private readonly List<JsonDiffNormalize> _rules = new List<JsonDiffNormalize>();

        public JsonDiffBuilder Use(JsonDiffNormalize normalize)
        {
            if (normalize == null)
                throw new ArgumentNullException(nameof(normalize));

            _rules.Add(normalize);
            return this;
        }

        public JsonDiffBuilder Use(JsonDiffPredicate match, JsonDiffNormalize normalize)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            if (normalize == null)
                throw new ArgumentNullException(nameof(normalize));

            _rules.Add((expected, actual, name, diff)
                => match(expected, actual, name)
                    ? normalize(expected, actual, name, diff)
                    : (expected, actual));

            return this;
        }

        public JsonDiff Build()
        {
            return new JsonDiff(_rules.ToArray());
        }
    }
}
