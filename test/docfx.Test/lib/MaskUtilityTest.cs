// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class MaskUtilityTest
    {
        [Fact]
        public static void HideSecret()
        {
            var githubToken = TestUtility.RandomSHA1Hash();
            var microsoftGraphClientCertificate = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var opBuildUserToken = Guid.NewGuid().ToString();

            var secrets = new[] { githubToken, microsoftGraphClientCertificate, opBuildUserToken };

            var data = new
            {
                githubToken,
                microsoftGraphClientCertificate,
                http = new Dictionary<string, object>
                {
                    {
                        "https://buildapi.docs.microsoft.com",
                        new { headers = new Dictionary<string, string> { ["X-OP-BuildUserToken"] = opBuildUserToken } }
                    },
                },
                arr = secrets,
            };

            var serialized = JsonUtility.Serialize(MaskUtility.HideSecret(JsonUtility.ToJObject(data)), indent: true);
            Assert.True(secrets.All(secret => !serialized.Contains(secret)));
            Assert.Contains("***", serialized);
        }
    }
}
