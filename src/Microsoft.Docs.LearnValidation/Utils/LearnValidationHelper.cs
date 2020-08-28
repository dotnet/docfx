// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;

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
        private const string _learnValidationAPIEndpoint = "/route/docs/api/hierarchy/";

        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _interceptHttpRequest;
        private readonly string _branch;

        public LearnValidationHelper(string branch, Func<HttpRequestMessage, Task<HttpResponseMessage>> interceptHttpRequest)
        {
            _interceptHttpRequest = interceptHttpRequest;
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
            if (_interceptHttpRequest == null)
            {
                return false;
            }

            var requestEndpoint = _learnValidationAPIEndpoint + (type == CheckItemType.Module ? $"modules/{uid}" : $"units/{uid}");

            var fallbackBranchs = _branch switch
            {
                "live" => new string[] { "live" },
                "master" => new string[] { "main", "master" },
                "main" => new string[] { "main", "master" },
                _ => new string[] { _branch, "main", "master" },
            };

            HttpResponseMessage response;
            foreach (var branch in fallbackBranchs)
            {
                response = CheckItemExistWithBranch(requestEndpoint, branch);

                Console.WriteLine("[LearnValidationPlugin] check {0} call: {1}", type, response.RequestMessage.RequestUri.AbsoluteUri);
                Console.WriteLine("[LearnValidationPlugin] check {0} result: {1}", type, response.IsSuccessStatusCode);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            return false;
        }

        private HttpResponseMessage CheckItemExistWithBranch(string endpoint, string branch)
        {
            var requestUrl = $"{endpoint}?branch={branch}&?locale={_defaultLocale}";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.TryAddWithoutValidation("Referer", "https://tcexplorer.azurewebsites.net");

            return _interceptHttpRequest(request).Result;
        }
    }
}
