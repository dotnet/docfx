// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class GitHubUser : ICacheObject<string>
    {
        public int? Id { get; set; }

        public string? Login { get; set; }

        public string? Name { get; set; }

        public string[] Emails { get; set; } = Array.Empty<string>();

        public DateTime? UpdatedAt { get; set; }

        public bool IsValid() => Id != null;

        public IEnumerable<string> GetKeys()
        {
            if (Login != null)
            {
                yield return Login;
            }

            foreach (var email in Emails)
            {
                yield return email;
            }
        }

        public Contributor ToContributor()
        {
            return new Contributor
            {
                Id = Id.ToString(),
                Name = Login,
                DisplayName = Name,
                ProfileUrl = "https://github.com/" + Login,
            };
        }
    }
}
