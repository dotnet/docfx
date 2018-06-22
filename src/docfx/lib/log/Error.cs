// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal class Error
    {
        public ErrorLevel Level { get; }

        public string Code { get; }

        public string Message { get; }

        public string File { get; }

        public int Line { get; }

        public int Column { get; }

        public Exception Exception { get; }

        public Error(
            ErrorLevel level,
            string code,
            string message,
            string file = null,
            int line = 0,
            int column = 0)
        {
            Debug.Assert(!string.IsNullOrEmpty(code));
            Debug.Assert(Regex.IsMatch(code, "^[a-z0-9-]{5,32}$"), "Error code should only contain dash and letters in lowercase");
            Debug.Assert(!string.IsNullOrEmpty(message));

            Level = level;
            Code = code;
            Message = message;
            File = file;
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            object[] payload = { Level, Code, Message, File, Line, Column };

            var i = payload.Length - 1;
            while (i >= 0 && (Equals(payload[i], null) || Equals(payload[i], "") || Equals(payload[i], 0)))
            {
                i--;
            }
            return JsonUtility.Serialize(payload.Take(i + 1));
        }

        public DocfxException ToException(Exception innerException = null)
        {
            return new DocfxException(this, innerException);
        }
    }
}
