// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
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
        private const string _learnValidationAPIEndpoint = "https://ops/learnvalidation/";

        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _interceptHttpRequest;
        private readonly string _branch;
        private readonly bool _needBranchFallback;
        private static readonly string[] _nofallbackBranches = new[] { "master", "live" };

        public LearnValidationHelper(string branch, Func<HttpRequestMessage, Task<HttpResponseMessage>> interceptHttpRequest)
        {
            _interceptHttpRequest = interceptHttpRequest;
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
            if (_interceptHttpRequest == null)
            {
                return false;
            }

            var requestEndpoint = _learnValidationAPIEndpoint + (type == CheckItemType.Module ? $"modules/{uid}" : $"units/{uid}");

            using var request = new HttpRequestMessage(HttpMethod.Get, requestEndpoint);
            request.Headers.TryAddWithoutValidation("Referer", "https://tcexplorer.azurewebsites.net");

            var (requestUrl, data) = CheckWithBranch(request, requestEndpoint, _branch);

            Console.WriteLine("[LearnValidationPlugin] check {0} call: {1}", type, requestUrl);
            Console.WriteLine("[LearnValidationPlugin] check {0} result: {1}", type, data);

            if (data == "{}" && _needBranchFallback)
            {
                (requestUrl, data) = CheckWithBranch(request, requestEndpoint, "master");

                Console.WriteLine("[LearnValidationPlugin] check {0} call: {1}", type, requestUrl);
                Console.WriteLine("[LearnValidationPlugin] check {0} result: {1}", type, data);
            }

            return data != "{}";
        }

        private (string requestUrl, string data) CheckWithBranch(HttpRequestMessage request, string endpoint, string branch)
        {
            request.RequestUri = new Uri(GetRequestUrl(branch));
            var response = _interceptHttpRequest(request).Result;
            var data = response.Content.ReadAsStringAsync().Result;
            return (request.RequestUri.AbsoluteUri, data);

            string GetRequestUrl(string branch) => $"{endpoint}?branch={branch}&?locale={_defaultLocale}";
        }
    }
}
