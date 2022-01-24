// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal static class GitHubQueries
{
    public const string UserQuery = @"
query ($login: String!) {
  user(login: $login) {
    name
    email
    databaseId
    login
  }
}";

    public const string CommitQuery = @"
query ($owner: String!, $name: String!, $commit: String!) {
  repository(owner: $owner, name: $name) {
    object(expression: $commit) {
      ... on Commit {
        history(first: 100) {
          nodes {
            author {
              email
              user {
                databaseId
                name
                email
                login
              }
            }
          }
        }
      }
    }
  }
}";
}
