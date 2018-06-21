// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class UserProfileCacheTest
    {
        private readonly Dictionary<string, GitUserProfile> _input =
            new Dictionary<string, GitUserProfile>
            {
                ["userA"] = new GitUserProfile
                {
                    ProfileUrl = "https://github.com/userA",
                    DisplayName = "User A",
                    Name = "userA",
                    Id = "1",
                    EmailAddress = "a@contoso.com",
                    UserEmails = "a@contoso.com;a@another.com"
                },
                ["userB"] = new GitUserProfile
                {
                    ProfileUrl = "https://github.com/userB",
                    DisplayName = "User B",
                    Name = "userB",
                    Id = "1",
                    EmailAddress = "b@contoso.com",
                    UserEmails = "b@contoso.com;b@another.com"
                },
            };

        [Fact]
        public void TestGetByUserName()
        {
            var cache = new GitUserProfileCache(_input);
            Assert.Equal("User A", cache.GetByUserName("userA").DisplayName);
            Assert.Equal("https://github.com/userB", cache.GetByUserName("userB").ProfileUrl);
            Assert.Null(cache.GetByUserName("unknown"));
        }

        [Fact]
        public void TestGetByUserEmail()
        {
            var cache = new GitUserProfileCache(_input);
            Assert.Equal("User A", cache.GetByUserEmail("a@contoso.com").DisplayName);
            Assert.Equal("User A", cache.GetByUserEmail("a@another.com").DisplayName);
            Assert.Equal("https://github.com/userB", cache.GetByUserEmail("b@contoso.com").ProfileUrl);
            Assert.Equal("https://github.com/userB", cache.GetByUserEmail("b@another.com").ProfileUrl);
            Assert.Null(cache.GetByUserEmail("unknown@contoso.com"));
        }
    }
}
