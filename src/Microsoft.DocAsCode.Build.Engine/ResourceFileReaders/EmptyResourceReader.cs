// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;
    using System.IO;

    public sealed class EmptyResourceReader : ResourceFileReader
    {
        private static readonly IEnumerable<string> Empty = new string[0];

        public override bool IsEmpty => true;
        public override string Name => "Empty";

        public override IEnumerable<string> Names => Empty;

        public override Stream GetResourceStream(string name)
        {
            return Stream.Null;
        }
    }
}
