using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class GitHubUserCacheTest
    {
        private static readonly Dictionary<string, TestCase> TestCases = PrepareTestCases();

        [Theory]
        [MemberData(nameof(GetData))]
        internal async Task TestCache(string name)
        {
            // Arrange
            var testCase = TestCases[name];
            var users = JsonUtility.Deserialize<GitHubUser[]>(testCase.CacheUsersJson.Replace("\'", "\""));
            var cache = new GitHubUserCache(users, "cache.json", 7 * 24);
            var accessor = new MockGitHubAccessor();
            cache._getUserByLoginFromGitHub = accessor.GetUserByLogin;
            cache._getUsersByCommitFromGitHub = accessor.GetUsersByCommit;

            // Act
            await testCase.Test(cache);

            // Assert
            Assert.Equal(testCase.ExpectedGetUserByLoginCall, accessor.GetUserByLoginCallCount);
            Assert.Equal(testCase.ExpectedGetLoginByCommitCall, accessor.GetLoginByCommitCallCount);
            var expectedUsers = JsonUtility.Deserialize<GitHubUser[]>(testCase.ExpectedOutputCacheUsersJson.Replace("\'", "\""));
            AssertUsersEqual(expectedUsers, cache.Users.ToArray());
        }

        public static TheoryData<string> GetData()
        {
            var result = new TheoryData<string>();
            foreach (var data in TestCases)
            {
                result.Add(data.Key);
            }
            return result;
        }

        private static Dictionary<string, TestCase> PrepareTestCases()
        {
            var result = new Dictionary<string, TestCase>();
            foreach (var data in PrepareDataCore())
            {
                result[(string)data[0]] =
                    new TestCase
                    {
                        Test = (Func<GitHubUserCache, Task>)data[1],
                        CacheUsersJson = (string)data[2],
                        ExpectedOutputCacheUsersJson = (string)data[3],
                        ExpectedGetUserByLoginCall = (int)data[4],
                        ExpectedGetLoginByCommitCall = (int)data[5],
                    };
            }
            return result;
        }

        private static IEnumerable<object[]> PrepareDataCore()
        {
            yield return new object[]
            {
                "Get user by login",
                 (Func<GitHubUserCache, Task>) ( async (cache) => await cache.GetByLogin("alice")),
                "[]",
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com']}]",
                1,
                0
            };
            yield return new object[]
            {
                "Get same user by login multiple times should call GitHub once",
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
                "Get user by login from cache",
                 (Func<GitHubUserCache, Task>) ( async (cache) => await cache.GetByLogin("alice")),
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com']}]",
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com']}]",
                0,
                0
            };
            yield return new object[]
            {
                "Get user by login from cache but cache expired",
                 (Func<GitHubUserCache, Task>) ( async (cache) => await cache.GetByLogin("alice")),
                "[{'id':1,'login':'alice','name':'Obsolete name of Alice','emails':['alice@contoso.com'],'expiry':'2000-01-01'}]",
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com']}]",
                1,
                0
            };
            yield return new object[]
            {
                "Get user by commit",
                (Func<GitHubUserCache, Task>) ( async (cache) => await cache.GetByCommit("alice@contoso.com", "owner", "name", "1")),
                "[]",
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com']}]",
                0,
                1
            };
            yield return new object[]
            {
                "Get same user by commit multiple times should call GitHub once",
                (Func<GitHubUserCache, Task>) ( async (cache) =>
                {
                    await ParallelUtility.ForEach(
                        Enumerable.Range(0, 20),
                        async (i) => await cache.GetByCommit("alice@contoso.com", "owner", "name", "1"));
                }),
                "[]",
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com']}]",
                0,
                1
            };
            yield return new object[]
            {
                "Get user by commit does not return user not found error",
                (Func<GitHubUserCache, Task>) ( async (cache) =>
                {
                    Assert.Null((await cache.GetByCommit("me@contoso.com", "owner", "name", "3")).error);
                    Assert.Null((await cache.GetByCommit("me@contoso.com", "owner", "name", "3")).error);
                }),
                "[]",
                "[{'emails':['me@contoso.com']}]",
                0,
                1
            };
            yield return new object[]
            {
                "Get user by commit does not cache email when commit is not resolved",
                (Func<GitHubUserCache, Task>) ( async (cache) => await cache.GetByCommit("me2@contoso.com", "not-exist", "not-eixts", "") ),
                "[]",
                "[]",
                0,
                1
            };
            yield return new object[]
            {
                "Get user by commit from cache",
                (Func<GitHubUserCache, Task>) ( async (cache) => await cache.GetByCommit("alice@contoso.com", "owner", "name", "1")),
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com']}]",
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com']}]",
                0,
                0
            };
            yield return new object[]
            {
                "Get user by commit from cache but cache expired",
                (Func<GitHubUserCache, Task>) ( async (cache) => await cache.GetByCommit("alice@contoso.com", "owner", "name", "1")),
                "[{'id':1,'login':'alice','name':'Obsolete name of Alice','emails':['alice@contoso.com'],'expiry':'2000-01-01'}]",
                "[{'id':1,'login':'alice','name':'Alice','emails':['alice@contoso.com']}]",
                0,
                1
            };
            yield return new object[]
            {
                "Get user by invalid login",
                (Func<GitHubUserCache, Task>) ( async (cache) => await cache.GetByLogin("invalid")),
                "[]",
                "[{'login':'invalid','emails':[]}]",
                1,
                0
            };
            yield return new object[]
            {
                "Get user by invalid login from cache",
               (Func<GitHubUserCache, Task>) ( async (cache) => await cache.GetByLogin("invalid")),
                "[{'login':'invalid','emails':[]}]",
                "[{'login':'invalid','emails':[]}]",
                0,
                0
            };
            yield return new object[]
            {
                "Dot not update cache when call GitHub API failed",
                (Func<GitHubUserCache, Task>)(async (cache) =>
                    {
                        await cache.GetByLogin("github-fail");
                        await cache.GetByCommit("github-fail@contoso.com", "owner", "name", "2");
                    }),
                "[]",
                "[]",
                1,
                1
            };
            yield return new object[]
            {
                "Prefer existing email name",
                (Func<GitHubUserCache, Task>)(async (cache) =>
                    {
                        await cache.GetByCommit("bob1@contoso.com", "owner", "name", "4");
                        await cache.GetByCommit("bob2@contoso.com", "owner", "name", "5");
                    }),
                "[]",
                "[{'id':2,'login':'bob','name':'bob1','emails':['bob1@contoso.com','bob2@contoso.com']}]",
                0,
                2
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
                        return Task.FromResult<(Error, GitHubUser)>((null, new GitHubUser { Id = 1, Login = "alice", Name = "Alice", Emails = new[] { "alice@contoso.com" } }));
                    case "github-fail":
                        return Task.FromResult<(Error, GitHubUser)>((Errors.GitHubApiFailed("API call failed for some reasons", new Exception()), null));
                    default:
                        return Task.FromResult<(Error, GitHubUser)>(default);
                }
            }

            public Task<(Error, IEnumerable<GitHubUser>)> GetUsersByCommit(string repoOwner, string repoName, string commitSha)
            {
                Interlocked.Increment(ref _getLoginByCommitCallCount);
                switch ($"{repoOwner}/{repoName}/{commitSha}")
                {
                    case "owner/name/1":
                        return Task.FromResult<(Error, IEnumerable<GitHubUser>)>((null, new[] { new GitHubUser { Id = 1, Login = "alice", Name = "Alice", Emails = new[] { "alice@contoso.com" } } }));
                    case "owner/name/2":
                        return Task.FromResult<(Error, IEnumerable<GitHubUser>)>((Errors.GitHubApiFailed("API call failed for some reasons", new Exception()), null));
                    case "owner/name/3":
                        return Task.FromResult<(Error, IEnumerable<GitHubUser>)>((null, new[] { new GitHubUser { Emails = new[] { "me@contoso.com" } } }));
                    case "owner/name/4":
                        return Task.FromResult<(Error, IEnumerable<GitHubUser>)>((null, new[] { new GitHubUser { Id = 2, Login = "bob", Name = "bob1", Emails = new[] { "bob1@contoso.com" } } }));
                    case "owner/name/5":
                        return Task.FromResult<(Error, IEnumerable<GitHubUser>)>((null, new[] { new GitHubUser { Id = 2, Login = "bob", Name = "bob2", Emails = new[] { "bob2@contoso.com" } } }));
                    default:
                        return Task.FromResult<(Error, IEnumerable<GitHubUser>)>(default);
                }
            }
        }

        private sealed class TestCase
        {
            public Func<GitHubUserCache, Task> Test { get; set; }
            public string CacheUsersJson { get; set; }
            public string ExpectedOutputCacheUsersJson { get; set; }
            public int ExpectedGetUserByLoginCall { get; set; }
            public int ExpectedGetLoginByCommitCall { get; set; }
        }
    }
}
