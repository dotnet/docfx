# GitHub User Cache

This document specifies how `docfx` caches GitHub user profiles to speedup contributor lookup.

## Introduction

When a docset is hosted on GitHub, `docfx` shows a list of GitHub accounts that have contributed to a document.
Contribution is determined by running `git log` on a document, than find all the authors that have committed
to that document.

GitHub accounts are identified by `login`. GitHub login is used widely in `docfx` to identify an user.
The value of _author_ in markdown YAML header is a GitHub login. [GitHub user API](https://developer.github.com/v3/users/#get-a-single-user) retrives a user by GitHub login.

In git, a commit contains author name, author email and commit hash. The commit author name is not the same as GitHub login name. [GitHub commit API](https://developer.github.com/v3/repos/commits/#get-a-single-commit) can tie a git commit to a GitHub account.

Due to performance reasons and [GitHub API rate limit](https://developer.github.com/v3/rate_limit/), it is not possible to call GitHub API every single time to resolve a contributor. Thus `docfx` caches these user profiles.

## Goals

Reduce calls to GitHub API as much as possible:

- Run `docfx` locally should reuse GitHub user cache on disk.
- Run `docfx` in a clustered server environment should reuse GitHub user cache as an HTTP resource.
- Run `docfx` locally should leverage external GitHub user cache as an HTTP resource.

## Data Structure

The GitHub user cache is a simple JSON file with a list of all GitHub user profiles.
Each profile has an random exipry to avoid expiration at the same time.
A profile can be a valid GitHub user, an email that does not exist or a login that does not exist.

```json
{
    "users": [
        { "login": "", "name": "", "email": "", "expiry": "" },
        { "login": "", "expiry": "" },
        { "email": "", "expiry": "" }
    ]
}
```

## Config

- `gitHubUserCache`: a config entry in _docfx.yml_ that sets the location of GitHub user cache. It could be a url or a file path.

- `DOCFX_GITHUB_USER_CACHE_WRITE_REMOTE`: a `boolean` environment variable indicating whether to write the cache back to the url or file specified in `gitHubUserCache`. Default to `false`.

- `DOCFX_GITHUB_USER_CACHE_WRITE_LOCAL`: a `boolean` environment variable indicating whether to write the cache back to _%USERPROFILE%/.docfx/cache/github-users.json_. Default to `true`.

## Scenarios

1. *Clustered Build Server*:
    - Cache is stored in a remote blob storage, `gitHubUserCache` is set to blob URL.
    - Set `DOCFX_GITHUB_USER_CACHE_WRITE_REMOTE` to `true`. When the cache is changed, upload it to the blob URL, handle merge conflicts using blob Etag.
    - Set `DOCFX_GITHUB_USER_CACHE_WRITE_LOCAL` to `false`.Does not update cache to local disk.

2. *Local Machine*:
    - Fetch and use a shared blob in configured in `gitHubUserCache`.
    - `DOCFX_GITHUB_USER_CACHE_WRITE_LOCAL` defaulted to `true`. Populate cache with _%USERPROFILE%/.docfx/cache/github-users.json_. When cache changed, write back to _%USERPROFILE%/.docfx/cache/github-users.json_.
    - `DOCFX_GITHUB_USER_CACHE_WRITE_REMOTE` defaulted to `false`, Do not update remote cache configured in `gitHubUserCache`.
