// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class GitHubUser
    {
        public int? Id { get; set; }

        public string Login { get; set; }

        public string Name { get; set; }

        public string[] Emails { get; set; } = Array.Empty<string>();

        public DateTime? Expiry { get; set; }

        public bool IsValid() => Id.HasValue;

        public bool IsExpired() => Expiry != null && Expiry < DateTime.UtcNow;

        public bool IsPartial() => !Id.HasValue && !string.IsNullOrEmpty(Login) && Emails.Length > 0;

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

        public void Merge(GitHubUser user)
        {
            Id = !user.IsValid() ? Id : user.Id;
            Login = string.IsNullOrEmpty(user.Login) ? Login : user.Login;
            Name = string.IsNullOrEmpty(user.Name) ? Name : user.Name;
            Emails = Emails.Concat(user.Emails).Distinct().ToArray();
            Expiry = Expiry > user.Expiry ? Expiry : user.Expiry;
        }
    }
}
