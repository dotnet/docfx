// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MarkdigEngine.Extensions
{
    using System.Collections.Generic;
    using System.IO;


    public class LineNumberExtensionContext
    {
        // This two private members are used for quickly getting the line number of one charactor
        // lineEnds[5] = 255 means the 6th lines ends at the 255th character of the text
        private int _previousLineNumber;
        private List<int> _lineEnds;

        internal string FilePath { get; private set; }

        public static LineNumberExtensionContext Create(string content, string absolutefilePath, string relativeFilePath)
        {
            var instance = new LineNumberExtensionContext()
            {
                FilePath = relativeFilePath
            };

            if (string.IsNullOrEmpty(content))
            {
                if (File.Exists(absolutefilePath))
                {
                    instance.ResetlineEnds(File.ReadAllText(absolutefilePath));
                }
            }
            else
            {
                instance.ResetlineEnds(content);
            }

            return instance;
        }

        private void ResetlineEnds(string text)
        {
            _previousLineNumber = 0;
            _lineEnds = new List<int>();
            for (int position = 0; position < text.Length; position++)
            {
                var c = text[position];
                if (c == '\r' || c == '\n')
                {
                    if (c == '\r' && position + 1 < text.Length && text[position + 1] == '\n')
                    {
                        position++;
                    }
                    _lineEnds.Add(position);
                }
            }
            _lineEnds.Add(text.Length - 1);
        }

        /// <summary>
        /// Should call ResetlineEnds() first, and call GetLineNumber with an incremental position
        /// </summary>
        /// <param name="position"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        internal int GetLineNumber(int position, int start)
        {
            int lineNumber = start > _previousLineNumber ? start : _previousLineNumber;

            if (_lineEnds == null || lineNumber >= _lineEnds.Count)
            {
                _previousLineNumber = start;
                return start;
            }

            for (; lineNumber < _lineEnds.Count; lineNumber++)
            {
                if (position <= _lineEnds[lineNumber])
                {
                    _previousLineNumber = lineNumber;
                    return lineNumber;
                }
            }

            _previousLineNumber = start;
            return start;
        }
    }
}
