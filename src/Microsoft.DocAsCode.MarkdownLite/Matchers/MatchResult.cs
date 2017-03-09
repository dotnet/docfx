// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    using System;
    using System.Collections.Generic;

    public class MatchResult
    {
        private readonly MatchContent _mc;
        public int Length { get; }

        public MatchResult(int length, MatchContent mc)
        {
            Length = length;
            _mc = mc;
        }

        public MatchGroup this[string name]
        {
            get
            {
                var group = GetGroup(name);
                if (group == null)
                {
                    throw new ArgumentException($"Group {name} not found.", nameof(name));
                }
                return group.Value;
            }
        }


        public MatchGroup? GetGroup(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            return _mc.GetGroup(name);
        }

        public IEnumerable<MatchGroup> EnumerateGroups() =>
            _mc.EnumerateGroups();
    }
}
