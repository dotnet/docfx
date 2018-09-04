// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class UserProfile
    {
        [JsonProperty("profile_url")]
        public string ProfileUrl { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("email_address")]
        public string EmailAddress { get; set; }

        [JsonProperty("user_emails")]
        public List<string> UserEmails { get; set; } = new List<string>();

        [JsonProperty("missing")]
        public bool Missing { get; set; } = false;

        public static UserProfile CreateNotFoundUserByEmail(string email)
        {
            Debug.Assert(!string.IsNullOrEmpty(email));

            return new UserProfile
            {
                EmailAddress = email,
                UserEmails = new List<string> { email },
                Missing = true,
            };
        }

        public static UserProfile CreateNotFoundUserByName(string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            return new UserProfile
            {
                Name = name,
                Missing = true,
            };
        }
    }
}
