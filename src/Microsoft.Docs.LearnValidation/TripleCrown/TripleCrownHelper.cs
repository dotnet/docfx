using Microsoft.OpenPublishing.PluginHelper;
using Microsoft.TripleCrown.DataContract.ViewModel;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TripleCrownValidation.TripleCrown
{
    public class TripleCrownHelper
    {
        private enum CheckItemType
        {
            Unit,
            Module
        };

        private RestClient _client;
        private string _branch;
        private bool _needBranchFallback;
        private const string _defaultLocale = "en-us";
        private static readonly string[] _nofallbackBranches = new[] { "master", "live" };

        public TripleCrownHelper(string endpoint, string branch)
        {
            _client = new RestClient(endpoint);
            _branch = branch;
            _needBranchFallback = !_nofallbackBranches.Contains(_branch);
        }

        public bool IsModule(string uid)
        {
            return CheckItemExist(CheckItemType.Module, uid);
        }

        public bool IsUnit(string uid)
        {
            return CheckItemExist(CheckItemType.Unit, uid);
        }

        private bool CheckItemExist(CheckItemType type, string uid)
        {
            var requestString = type == CheckItemType.Module ? $"modules/{uid}" : $"units/{uid}";
            var request = new RestRequest(requestString);

            AppendHeaderAndParameter(request);

            IRestResponse response = _client.Execute(request);

            OPSLogger.LogToConsole("TripleCrownValidation check {0} call: {1}", type, response.ResponseUri);
            OPSLogger.LogToConsole("TripleCrownValidation check {0} result: {1}", type, response.StatusCode);

            if (response.StatusCode != HttpStatusCode.OK && _needBranchFallback)
            {
                request.AddOrUpdateParameter("branch", "master");
                response = _client.Execute(request);

                OPSLogger.LogToConsole("TripleCrownValidation check {0} call: {1}", type, response.ResponseUri);
                OPSLogger.LogToConsole("TripleCrownValidation check {0} result: {1}", type, response.StatusCode);
            }

            return response.StatusCode == HttpStatusCode.OK;
        }

        private void AppendHeaderAndParameter(RestRequest request)
        {
            request.AddParameter("branch", _branch);
            request.AddParameter("locale", _defaultLocale);
            request.AddHeader("Referer", " https://tcexplorer.azurewebsites.net");
        }
    }
}
