// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;

    public sealed class XRefMapReader : XRefRedirectionReader
    {
        private readonly Dictionary<string, IXRefContainer> _maps;

        public XRefMapReader(string majorKey, Dictionary<string, IXRefContainer> maps)
            : base(majorKey, new HashSet<string>(maps.Keys))
        {
            _maps = maps;
        }

        protected override IXRefContainer GetMap(string name)
        {
            _maps.TryGetValue(name, out IXRefContainer result);
            return result;
        }
    }
}
