using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    public interface IBuildController
    {
        MetadataItem ExtractMetadata(IInputParameters parameters);
    }
}
