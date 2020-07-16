using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripleCrownValidation.XRef
{
    public class XRefResult
    {
        [JsonProperty("uid")]
        public string Uid { get; set; }
        [JsonProperty("href")]
        public string Href { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
    }
}
