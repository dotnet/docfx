using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripleCrownValidation.Models
{
    public interface IValidateModel
    {
        string Uid { get; }
        string SourceRelativePath { get; set; }
        bool IsValid { get; set; }
        IValidateModel Parent { get; set; }
        bool IsDeleted { get; set; }
        string MSDate { get; set; }
        string ServiceData { get; set; }
        string PublishUpdatedAt { get; set; }
        string PageKind { get; set; }
        string AssetId { get; set; }
    }
}
