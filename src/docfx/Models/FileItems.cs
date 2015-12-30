// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class FileItems : List<string>
    {
        private static IEnumerable<string> Empty = new List<string>();
        public FileItems(string file) : base()
        {
            this.Add(file);
        }

        public FileItems(IEnumerable<string> files) : base(files ?? Empty)
        {
        }

        public static explicit operator FileItems(string input)
        {
            return new FileItems(input);
        }
    }
}
