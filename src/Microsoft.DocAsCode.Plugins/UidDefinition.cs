// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    public class UidDefinition
    {
        public string Name { get; }
        public string File { get; }
        public int? Line { get; }
        public int? Column { get; }

        public UidDefinition(string uid, string file, int? line = null, int? column = null)
        {
            Name = uid;
            File = file;
            Line = line;
            Column = column;
        }
    }
}
