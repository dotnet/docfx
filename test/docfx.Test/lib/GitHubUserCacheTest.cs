using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class GitHubUserCacheTest
    {
        [Fact]
        public void GetUserByLogin()
        {
            VerifyCore(
                async (cache) => await cache.GetByLogin("alice"),
                "{}",
                "{\"users\":[{\"id\":1,\"login\":\"alice\",\"name\":\"Alice\",\"emails\":[\"alice@contoso.com\"]}]}",
                1,
                0);
        }

        [Fact]
        public void GetSameUserByLoginMultipleTimesShouldCallGitHubOnce()
        {
            VerifyCore(
                async (cache) =>
                {
                    await cache.GetByLogin("alice");
                    await cache.GetByLogin("alice");
                },
                "{}",
                "{\"users\":[{\"id\":1,\"login\":\"alice\",\"name\":\"Alice\",\"emails\":[\"alice@contoso.com\"]}]}",
                1,
                0);
        }

        [Fact]
        public void GetUserByLoginFromCache()
        {
            VerifyCore(
                async (cache) => await cache.GetByLogin("alice"),
                "{\"users\":[{\"id\":1,\"login\":\"alice\",\"name\":\"Alice\",\"emails\":[\"alice@contoso.com\"]}]}",
                "{\"users\":[{\"id\":1,\"login\":\"alice\",\"name\":\"Alice\",\"emails\":[\"alice@contoso.com\"]}]}",
                0,
                0);
        }

        [Fact]
        public void GetUserByCommmit()
        {
            VerifyCore(
                async (cache) => await cache.GetByCommit("alice@contoso.com", "owner", "name", "1"),
                "{}",
                "{\"users\":[{\"id\":1,\"login\":\"alice\",\"name\":\"Alice\",\"emails\":[\"alice@contoso.com\"]}]}",
                1,
                1);
        }

        [Fact]
        public void GetSameUserByCommmitMultipleTimesShouldCallGitHubOnce()
        {
            VerifyCore(
                async (cache) =>
                {
                    await cache.GetByCommit("alice@contoso.com", "owner", "name", "1");
                    await cache.GetByCommit("alice@contoso.com", "owner", "name", "2");
                },
                "{}",
                "{\"users\":[{\"id\":1,\"login\":\"alice\",\"name\":\"Alice\",\"emails\":[\"alice@contoso.com\"]}]}",
                1,
                1);
        }

        [Fact]
        public void GetUserByCommmitFromCache()
        {
            VerifyCore(
                async (cache) => await cache.GetByCommit("alice@contoso.com", "owner", "name", "1"),
                "{\"users\":[{\"id\":1,\"login\":\"alice\",\"name\":\"Alice\",\"emails\":[\"alice@contoso.com\"]}]}",
                "{\"users\":[{\"id\":1,\"login\":\"alice\",\"name\":\"Alice\",\"emails\":[\"alice@contoso.com\"]}]}",
                0,
                0);
        }

        [Fact]
        public void GetUserByCommmitWithNewEmail()
        {
            VerifyCore(
                async (cache) => await cache.GetByCommit("alice_new@contoso.com", "owner", "name", "1"),
                "{}",
                "{\"users\":[{\"id\":1,\"login\":\"alice\",\"name\":\"Alice\",\"emails\":[\"alice@contoso.com\",\"alice_new@contoso.com\"]}]}",
                1,
                1);
        }

        [Fact]
        public void GetUserByCommmitWithNewEmailCanCompleteCache()
        {
            VerifyCore(
                async (cache) => await cache.GetByCommit("alice_new@contoso.com", "owner", "name", "1"),
                "{\"users\":[{\"id\":1,\"login\":\"alice\",\"name\":\"Alice\",\"emails\":[\"alice@contoso.com\"]}]}",
                "{\"users\":[{\"id\":1,\"login\":\"alice\",\"name\":\"Alice\",\"emails\":[\"alice@contoso.com\",\"alice_new@contoso.com\"]}]}",
                0,
                1);
        }

        [Fact]
        public void GetUserByInvalidLogin()
        {
            VerifyCore(
                async (cache) => await cache.GetByLogin("invalid"),
                "{}",
                "{\"users\":[{\"login\":\"invalid\",\"emails\":[]}]}",
                1,
                0); ;
        }
        [Fact]
        public void GetUserByInvalidLoginFromCache()
        {
            VerifyCore(
                async (cache) => await cache.GetByLogin("invalid"),
                "{\"users\":[{\"login\":\"invalid\",\"emails\":[]}]}",
                "{\"users\":[{\"login\":\"invalid\",\"emails\":[]}]}",
                0,
                0); ;
        }

        private async void VerifyCore(Action<GitHubUserCache> test, string inputCache, string expectedOutputCache, int expectedGetUserByLoginCall, int expectedGetLoginByCommitCall)
        {
            // Arrange
            var docsetPath = Path.Combine("github-user-cache-test", Path.GetRandomFileName());
            if (Directory.Exists(docsetPath))
                Directory.Delete(docsetPath, true);
            Directory.CreateDirectory(docsetPath);
            var docfxYmlPath = Path.Combine(docsetPath, "docfx.yml");
            var gitHubUserCachePath = Path.Combine(docsetPath, "cache.json");
            File.WriteAllText(docfxYmlPath, @"gitHub:
  userCache: cache.json");
            File.WriteAllText(gitHubUserCachePath, inputCache);

            var (_, config) = Config.Load("docset", new CommandLineOptions());
            var options = new CommandLineOptions();
            var docset = new Docset(new Context(new Report(), "_site"), docsetPath, config, options);
            var cache = await GitHubUserCache.Create(docset, null);
            var accessor = new MockGitHubAccessor();
            cache._getUserByLoginFromGitHub = accessor.GetUserByLogin;
            cache._getLoginByCommitFromGitHub = accessor.GetLoginByCommit;

            // Act
            test(cache);
            await cache.SaveChanges();

            // Assert
            var outputCache = File.ReadAllText(gitHubUserCachePath);
            Assert.Equal(expectedGetUserByLoginCall, accessor.GetUserByLoginCallCount);
            Assert.Equal(expectedGetLoginByCommitCall, accessor.GetLoginbyCommitCallCount);
            AssertCacheEqual(expectedOutputCache, outputCache);

            Directory.Delete(docsetPath, true);
        }

        private void AssertCacheEqual(string expected, string actual)
        {
            var (_, expectedObj) = JsonUtility.Deserialize<JObject>(expected);
            var (_, actualObj) = JsonUtility.Deserialize<JObject>(actual);
            var expectedUsers = expectedObj["users"] as JArray;
            var actualUsers = actualObj["users"] as JArray;
            Assert.NotNull(expectedUsers);
            Assert.NotNull(actualUsers);

            Assert.Equal(expectedUsers.Count, actualUsers.Count);
            for (var i = 0; i < expectedUsers.Count; i++)
            {
                var expectedUser = expectedUsers[i] as JObject;
                var actualUser = actualUsers[i] as JObject;

                expectedUser["emails"] = new JArray(expectedUser["emails"].OrderBy(e => e));
                actualUser["emails"] = new JArray(actualUser["emails"].OrderBy(e => e));
                actualUser.Remove("expiry");

                Assert.True(JToken.DeepEquals(expectedUser, actualUser));
            }
        }

        private sealed class MockGitHubAccessor
        {
            public int GetUserByLoginCallCount { get; private set; }

            public int GetLoginbyCommitCallCount { get; private set; }

            public Task<(Error, GitHubUser)> GetUserByLogin(string login)
            {
                GetUserByLoginCallCount++;
                switch (login)
                {
                    case "alice":
                        return Task.FromResult<(Error, GitHubUser)>((null, new GitHubUser() { Id = 1, Login = "alice", Name = "Alice", Emails = new[] { "alice@contoso.com" } }));
                    default:
                        return Task.FromResult((Errors.GitHubUserNotFound(login), new GitHubUser() { Login = login }));
                }
            }

            public Task<(Error, string logiin)> GetLoginByCommit(string repoOwner, string repoName, string commitSha)
            {
                GetLoginbyCommitCallCount++;
                switch ($"{repoOwner}/{repoName}/{commitSha}")
                {
                    case "owner/name/1":
                    case "owner/name/2":
                        return Task.FromResult<(Error, string)>((null, "alice"));
                    default:
                        return Task.FromResult<(Error, string)>(default);
                }
            }
        }
    }
}
