// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    public struct Location
    {
        public Coordinate StartLocation { get; set; }

        public Coordinate EndLocation { get; set; }

        public static Location GetLocation(string input, int matchedStartIndex, int matchedLength)
        {
            if (matchedLength <= 0) return new Location();
            if (matchedStartIndex < 0) matchedStartIndex = 0;
            if (matchedStartIndex + matchedLength > input.Length)
            {
                matchedLength = input.Length - matchedStartIndex;
            }

            var beforeMatch = input.Substring(0, matchedStartIndex);
            Coordinate start = Coordinate.GetCoordinate(beforeMatch);

            var matched = input.Substring(matchedStartIndex, matchedLength);
            Coordinate startToEnd = Coordinate.GetCoordinate(matched);
            Coordinate end = start.Add(startToEnd);
            return new Location { StartLocation = start, EndLocation = end };
        }

        public bool IsIn(Location wrapper)
        {
            return wrapper.StartLocation.CompareTo(this.StartLocation) <= 0 && wrapper.EndLocation.CompareTo(this.EndLocation) >= 0;
        }

        public override string ToString()
        {
            return string.Format("{Start{0}, End{1}}", StartLocation, EndLocation);
        }
    }
}
