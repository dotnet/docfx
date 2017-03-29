// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    using System;

    internal sealed class LengthComparisonMatcher : Matcher
    {
        private readonly string _groupName;
        private readonly Matcher _inner;
        private readonly LengthComparison _comparsion;

        public LengthComparisonMatcher(Matcher inner, LengthComparison comparsion, string groupName)
        {
            _inner = inner;
            _comparsion = comparsion;
            _groupName = groupName;
        }

        public override int Match(MatchContent content)
        {
            var count = _inner.Match(content);
            if (count == NotMatch)
            {
                return NotMatch;
            }
            var g = content.GetGroup(_groupName);
            if (g == null)
            {
                return NotMatch;
            }
            switch (_comparsion)
            {
                case LengthComparison.Equals:
                    return count == g.Value.Count ? count : NotMatch;
                case LengthComparison.LessThan:
                    return count < g.Value.Count ? count : NotMatch;
                case LengthComparison.GreaterThan:
                    return count > g.Value.Count ? count : NotMatch;
                case LengthComparison.LessThanOrEquals:
                    return count <= g.Value.Count ? count : NotMatch;
                case LengthComparison.GreaterThanOrEquals:
                    return count >= g.Value.Count ? count : NotMatch;
                default:
                    throw new InvalidOperationException();
            }
        }

        public override string ToString()
        {
            return "(Length:" + _comparsion.ToString() + "<" + _groupName + ">)";
        }
    }
}
