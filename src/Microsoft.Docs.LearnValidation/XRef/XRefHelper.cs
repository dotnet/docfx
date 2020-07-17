// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TripleCrownValidation
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
    }
}
