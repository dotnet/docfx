using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class GitHubUserCacheTest
    {
        [Theory]
        [MemberData(nameof(GetData))]
        internal async Task VerifyOutputcache(
            Func<GitHubUserCache, Task> test,
            string cacheUsersJson,
            string expectedOutputCacheUsersJson,
            int expectedGetUserByLoginCall,
            int expectedGetLoginByCommitCall)
        {
            // Arrange
            var users = JsonUtility.Deserialize<GitHubUser[]>(cacheUsersJson.Replace("\'", "\""));
            var cache = new GitHubUserCache(users, "cache.json", 7 * 24);
            var accessor = new MockGitHubAccessor();
            cache._getUserByLoginFromGitHub = accessor.GetUserByLogin;
            cache._getLoginByCommitFromGitHub = accessor.GetLoginByCommit;

            // Act
            await test(cache);

            // Assert
            Assert.Equal(expectedGetUserByLoginCall, accessor.GetUserByLoginCallCount);
            Assert.Equal(expectedGetLoginByCommitCall, accessor.GetLoginByCommitCallCount);
            var expectedUsers = JsonUtility.Deserialize<GitHubUser[]>(expectedOutputCacheUsersJson.Replace("\'", "\""));
            AssertUsersEqual(expectedUsers, cache.Users.ToArray());
        }

        public static IEnumerable<object[]> GetData()
        {
            yield return new object[]
            {
                 (Func<GitHubUserCache, Task>) ( async (cache) => await cache.GetByLogin("alice")),
                "[]",
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com']}]",
                1,
                0
            };
            yield return new object[]
            {
                (Func<GitHubUserCache, Task>)(async (cache) =>
                    {
                        await ParallelUtility.ForEach(Enumerable.Range(0, 20), async (_) => await cache.GetByLogin("alice"));
                    }),
                "[]",
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com']}]",
                1,
                0
            };
            yield return new object[]
            {
                 (Func<GitHubUserCache, Task>) ( async (cache) => await cache.GetByLogin("alice")),
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com']}]",
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com']}]",
                0,
                0
            };
            yield return new object[]
            {
                (Func<GitHubUserCache, Task>) ( async (cache) => await cache.GetByCommit("alice@contoso.com", "owner", "name", "1")),
                "[]",
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com']}]",
                1,
                1
            };
            yield return new object[]
            {
                (Func<GitHubUserCache, Task>) ( async (cache) =>
                {
                    await ParallelUtility.ForEach(
                        Enumerable.Range(0, 20),
                        async (i) => await cache.GetByCommit("alice@contoso.com", "owner", "name", "1"));
                }),
                "[]",
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com']}]",
                1,
                1
            };
            yield return new object[]
            {
                (Func<GitHubUserCache, Task>) ( async (cache) => await cache.GetByCommit("alice@contoso.com", "owner", "name", "1")),
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com']}]",
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com']}]",
                0,
                0
            };
            yield return new object[]
            {
                (Func<GitHubUserCache, Task>) ( async (cache) => await cache.GetByCommit("alice_new@contoso.com", "owner", "name", "1")),
                "[]",
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com','alice_new@contoso.com']}]",
                1,
                1
            };
            yield return new object[]
            {
                (Func<GitHubUserCache, Task>) ( async (cache) => await cache.GetByCommit("alice_new@contoso.com", "owner", "name", "1")),
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com']}]",
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com','alice_new@contoso.com']}]",
                0,
                1
            };
            yield return new object[]
            {
                (Func<GitHubUserCache, Task>) ( async (cache) => await cache.GetByLogin("invalid")),
                "[]",
                "[{'login':'invalid','emails':[]}]",
                1,
                0
            };
            yield return new object[]
            {
               (Func<GitHubUserCache, Task>) ( async (cache) => await cache.GetByLogin("invalid")),
                "[{'login':'invalid','emails':[]}]",
                "[{'login':'invalid','emails':[]}]",
                0,
                0
            };
        }

        private void AssertUsersEqual(GitHubUser[] expectedUsers, GitHubUser[] actualUsers)
        {
            Assert.Equal(expectedUsers.Length, actualUsers.Length);
            for (var i = 0; i < expectedUsers.Length; i++)
            {
                AssertUserEqual(expectedUsers[i], actualUsers[i]);
            }
        }

        private void AssertUserEqual(GitHubUser expectedUser, GitHubUser actualUser)
        {
            Assert.Equal(expectedUser.Id, actualUser.Id);
            Assert.Equal(expectedUser.Login, actualUser.Login);
            Assert.Equal(expectedUser.Name, actualUser.Name);
            Assert.True(expectedUser.Emails.OrderBy(u => u).SequenceEqual(actualUser.Emails.OrderBy(u => u)));
        }

        private sealed class MockGitHubAccessor
        {
            public int GetUserByLoginCallCount { get { return _getUserByLoginCallCount; } }

            public int GetLoginByCommitCallCount { get { return _getLoginByCommitCallCount; } }

            private int _getUserByLoginCallCount;
            private int _getLoginByCommitCallCount;

            public Task<(Error, GitHubUser)> GetUserByLogin(string login)
            {
                Interlocked.Increment(ref _getUserByLoginCallCount);
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
                Interlocked.Increment(ref _getLoginByCommitCallCount);
                switch ($"{repoOwner}/{repoName}/{commitSha}")
                {
                    case "owner/name/1":
                        return Task.FromResult<(Error, string)>((null, "alice"));
                    default:
                        return Task.FromResult<(Error, string)>(default);
                }
            }
        }
    }
}
