using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripleCrownValidation.Models
{
    public class ModuleValidateModel : ModuleSyncModel, IValidateModel
    {
        [JsonProperty("source_relative_path")]
        public string SourceRelativePath { get; set; }

        public bool IsValid { get; set; }

        public bool IsDeleted { get; set; }

        public string Uid => UId;

        public IValidateModel Parent { get; set; }
        
        public string MSDate { get; set; }
        
        public string ServiceData { get; set; }
        
        public string PublishUpdatedAt { get; set; }
        
        public string PageKind { get; set; }
    }
}
