// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
        public string UserEmails { get; set; }

        public string[] GetUserEmails() => UserEmails != null ? UserEmails.Split(";") : Array.Empty<string>();

        public UserProfile AddEmail(string email)
        {
            return new UserProfile
            {
                ProfileUrl = ProfileUrl,
                DisplayName = DisplayName,
                Name = Name,
                Id = Id,
                EmailAddress = EmailAddress,
                UserEmails = Merge(GetUserEmails(), email),
            };

            string Merge(string[] existingEmails, string newEmail)
            {
                var result = new HashSet<string>(existingEmails)
                {
                    newEmail,
                };

                // TODO: avoid split+join multiple times
                return string.Join(';', result);
            }
        }
    }
}
