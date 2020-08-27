// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net;
using RestSharp;

namespace Microsoft.Docs.LearnValidation
{
    public class LearnValidationHelper
    {
        private enum CheckItemType
        {
            Unit,
            Module,
        }

        private const string _defaultLocale = "en-us";

        private readonly RestClient _client;
        private readonly string _branch;

        public LearnValidationHelper(string endpoint, string branch)
        {
            _client = string.IsNullOrEmpty(endpoint) ? null : new RestClient(endpoint);
            _branch = branch;
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
            if (_client == null)
            {
                return false;
            }

            var requestString = type == CheckItemType.Module ? $"modules/{uid}" : $"units/{uid}";
            var request = new RestRequest(requestString);

            AppendHeaderAndParameter(request);

            var fallbackBranchs = _branch switch
            {
                "live" => new string[] { "live" },
                "master" => new string[] { "main", "master" },
                "main" => new string[] { "main", "master" },
                _ => new string[] { _branch, "main", "master" },
            };

            IRestResponse response;
            foreach (var branch in fallbackBranchs)
            {
                request.AddOrUpdateParameter("branch", branch);
                response = _client.Execute(request);

                Console.WriteLine("[LearnValidationPlugin] check {0} call: {1}", type, response.ResponseUri);
                Console.WriteLine("[LearnValidationPlugin] check {0} result: {1}", type, response.StatusCode);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
            }
            return false;
        }

        private void AppendHeaderAndParameter(RestRequest request)
        {
            request.AddParameter("branch", _branch);
            request.AddParameter("locale", _defaultLocale);
            request.AddHeader("Referer", " https://tcexplorer.azurewebsites.net");
        }
    }
}
