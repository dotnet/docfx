// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Linq;

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
            int line = content.Count(c => c == NewLineCharacter);

            int lineStart = content.LastIndexOf(NewLineCharacter, index);

            int col = index - lineStart;
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
