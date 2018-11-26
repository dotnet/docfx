// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;
    
    public class AbstractProjectLoader
    {
        IEnumerable<IProjectLoader> _loaders;

        public AbstractProjectLoader(IEnumerable<IProjectLoader> loaders)
        {
            _loaders = loaders;
        }

        public AbstractProject Load(string path)
        {
            foreach (var loader in _loaders)
            {
                var p = loader.TryLoad(path, this);
                if (p != null)
                    return p;
            }
            return null;
        }
    }
}
