using Microsoft.OpenPublishing.PluginHelper;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripleCrownValidation.XRef
{
    public class XRefHelper
    {
        private RestClient _client;
        private string _tags;

        public XRefHelper(string endpoint, string tags)
        {
            _client = new RestClient(endpoint);
            _tags = tags;
        }

        public XRefResult[] ResolveUid(string uid)
        {
            var request = new RestRequest("query");
            request.AddParameter("uid", uid);
            request.AddParameter("tags", _tags);
            IRestResponse response = _client.Execute(request);
            var content = response.Content; // raw content as string      

            OPSLogger.LogToConsole("TripleCrownValidation resolveUid call: {0}", _client.BuildUri(request));
            OPSLogger.LogToConsole("TripleCrownValidation resolveUid result: {0}", content);
            if (!string.IsNullOrEmpty(content))
            {
                return JsonConvert.DeserializeObject<XRefResult[]>(content);
            }
            return null;
        }
    }
}
