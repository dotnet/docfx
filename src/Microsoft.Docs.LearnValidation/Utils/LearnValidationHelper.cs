// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;

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

        private readonly ILearnServiceAccessor _learnServiceAccessor;
        private readonly string _branch;

        public LearnValidationHelper(string branch, ILearnServiceAccessor learnServiceAccessor)
        {
            _learnServiceAccessor = learnServiceAccessor;
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
            if (_learnServiceAccessor == null)
            {
                return false;
            }

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
                response = _learnServiceAccessor.CheckLearnPathItemExist(branch, _defaultLocale, uid, type == CheckItemType.Module).Result;

                Console.WriteLine("[LearnValidationPlugin] check {0} call: {1}", type, response.RequestMessage.RequestUri.AbsoluteUri);
                Console.WriteLine("[LearnValidationPlugin] check {0} result: {1}", type, response.IsSuccessStatusCode);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
