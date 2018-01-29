using System.Collections.Generic;

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
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
