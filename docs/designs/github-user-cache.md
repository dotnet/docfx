# GitHub User Cache

This document specifies how `docfx` caches GitHub user profiles to speedup contributor lookup.

## Introduction

When a docset is hosted on GitHub, `docfx` shows a list of GitHub accounts that have contributed to a document.
Contribution is determined by running `git log` on a document, then find all the authors that have committed
to that document.

GitHub accounts are identified by `login`. GitHub login is used widely in `docfx` to identify an user.
The value of _author_ in markdown YAML header is a GitHub login. [GitHub user API](https://developer.github.com/v3/users/#get-a-single-user) retrieves a user by GitHub login.

In git, a commit contains author name, author email and commit hash. The commit author name is not the same as GitHub login name. [GitHub commit API](https://developer.github.com/v3/repos/commits/#get-a-single-commit) can tie a git commit to a GitHub account.

Due to performance reasons and [GitHub API rate limit](https://developer.github.com/v3/rate_limit/), it is not possible to call GitHub API every single time to resolve a contributor. `docfx` caches GitHub user profiles.

## Data Structure

The GitHub user cache is a simple JSON file with a list of all GitHub user profiles.
Each profile has an random exipry to avoid expiration at the same time.
A profile can be a valid GitHub user, an email that does not exist or a login that does not exist.

```json
{
    "users": [
        { "login": "", "name": "", "email": "", "expires": "" },
        { "login": "", "expires": "" },
        { "email": "", "expires": "" }
    ]
}
```

## Configuration

- `resolveGitHubUser`: a boolean config to toggle whether git commit users are resolved into GitHub users. Default to `false`.

- `gitHubUserCache`: an config that sets the file path of the cache. Default to `%USERPROFILE%/.docfx/cache/github-users.json`.

## Scenarios

- *Local Machine*:
    - Do not resolve GitHub users, local preview shows name from git and default profile placeholder images.

- *Clustered Build Server*:
    - Set `resolveGitHubUser` to `true` to resolve contributors.
    - GitHub user cache is currently only shared within a single build machine.
