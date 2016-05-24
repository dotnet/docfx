// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;

    public sealed class XRefMapReader : XRefRedirectionReader
    {
        private readonly Dictionary<string, XRefMap> _maps;

        public XRefMapReader(string majorKey, Dictionary<string, XRefMap> maps)
            : base(majorKey, new HashSet<string>(maps.Keys))
        {
            _maps = maps;
        }

        protected override XRefMap GetMap(string name)
        {
            XRefMap result;
            _maps.TryGetValue(name, out result);
            return result;
        }
    }
}
