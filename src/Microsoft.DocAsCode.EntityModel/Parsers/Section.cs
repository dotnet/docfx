// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Newtonsoft.Json;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Utility;

    public class Section
    {
        /// <summary>
        /// The raw content matching the regular expression, e.g. @ABC
        /// </summary>
        [YamlDotNet.Serialization.YamlMember(Alias = "key")]
        [JsonProperty("key")]
        public string Key { get; set; }

        /// <summary>
        /// Defines the Markdown Content Location Range
        /// </summary>
        [YamlDotNet.Serialization.YamlIgnore]
        [JsonIgnore]
        public List<Location> Locations { get; set; }
    }

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
            return new Location() { StartLocation = start, EndLocation = end };
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

    public struct Coordinate : IComparable<Coordinate>
    {
        private const char NewLineCharacter = '\n';

        public int Line { get; set; }
        public int Column { get; set; }

        public readonly static Coordinate Default = new Coordinate();

        public Coordinate Add(Coordinate toAdd)
        {
            return new Coordinate
            {
                Line = this.Line + toAdd.Line,
                Column = toAdd.Line == 0 ? this.Column + toAdd.Column : toAdd.Column
            };
        }

        /// <summary>
        /// Lines and Columns start at 0 to leverage default value, NOTE that IDE start at 1, need to +1 at the outermost
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static Coordinate GetCoordinate(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return Default;
            }
            int index = content.Length - 1;
            int line = content.Count(c => c == NewLineCharacter); // Assume there is no new line at the end of the file, count(line) = count(newline) + 1

            // Remove last new line character if it is last character of the content
            if (content[content.Length - 1] == NewLineCharacter)
            {
                line--;
                index--;
            }
            int lineStart = content.LastIndexOf(NewLineCharacter, index);
            int col = index - lineStart - 1;
            return new Coordinate { Line = line, Column = col };
        }

        public int CompareTo(Coordinate other)
        {
            var lineDiff = Line - other.Line;
            if (lineDiff != 0)
            {
                return lineDiff;
            }
            return Column - other.Column;
        }

        public override string ToString()
        {
            return string.Format("{{line{0}, col{1}}}", Line.ToString(), Column.ToString());
        }
    }
}
