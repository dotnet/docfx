# Restore

Docfx `restore` helps you to restore all you dependency repositories/files(defined in [config](ocs/designs/config.md), fallback source repository and theme repository) into your local `%DOCFX_APPDATA_PATH%` for build preparation.

The default `%DOCFX_APPDATA_PATH%` is `%USERPROFILE%/.docfx`, but you can reset it to any other place.

The restore need make sure:

  - Once one version of file/repository is restored, this version of the file/repository should NOT be updated until be cleaned up by `GC`. To make sure the `build`s which using this version are not broken causing by `restore`.
  - Provide a mechanism to lock down the dependency file/repository version for different `build`s, just like [npm package locks](https://docs.npmjs.com/files/package-locks)

## Restore files

There are some cases that the docfx `build` depends on external files for help. 

For example, if you want the docfx to resolve `github contributors` from building file git commit history, the `gitHub.userCache` file can be set up to reduce the API calls to [GitHub](https://github.com). The file can be just a http(s) url, which `restore` will help to download for `build` usage.

```yml
github:
  userCache: https:///some-blob-service/github-user-cache.json
```

The restored files will be stored at `%DOCFX_APPDATA_PATH%/downloads/{url-short-path}/{version}+{etage}` like:

```text
`%DOCFX_APPDATA_PATH%`
    | - downloads
        | - raw.gith..tent.com+docascode+docfx-te..ndencies+62b0448+extend1.yml+dc363b0e
            | - 18b265bc4e9a6b4fdff8b5210caf9e3541f4cb2d+%22f02fde5b4f3817266458170ba048c7fc2de46287%22
            | - 3111eb522b092c9cef5cd0ccd86892cf847592d6+%2299a5dbb8d73da1ec452d74c0ab45b11ebe8615a0%22

```

- `{url-short-path}` is calculated from `file` url
- `{version}` is sha1 hash of file content, 
- `{etage}` is timestamp or something else supported by the service which stores the `file`.

If the `etag` is supported by its service, docfx avoids duplicated downloads for the file never changed.


## Restore dependency repository

There are some cases that the docfx `build` depends on external repositories, which includes `tokens`, `code-snippets` or other resources/content.

For example, some files are referencing `code example` for better explanations, but some of the code examples stores in another repository, maintained by another team/owners, with the dependency repository, you have the ability to reference external `code example` to be built in your page.

Config(ocs/designs/config.md):

``` yml
dependencies:
  docfx-code: https://github.com/dotnet/docfx#master
```

Content:

```md
Example: [!code-csharp[Main](docfx-code/program.cs)]
```

The restored dependency repositories will be stored at `%DOCFX_APPDATA_PATH%/git/{url-short-path}/{worktree-name}` organized by [git work tree](https://git-scm.com/docs/git-worktree) like:

```text
`%DOCFX_APPDATA_PATH%`
    | - git
        | - github.com+Microsoft+Luis-Samples+5e455995
            | - .git
            | - master-fd6968dec9a9a39aec1845232466fe35fca520da
            | - live-a82cb2a51f6925b2fa87275218b01678c5aadfe1
```

- `{url-short-path}` is calculated from `file` url
- `{worktree-name}` is combined by `branch name` and `HEAD commit id`

# Dependency locks

There are some cases you want to keep using the **same version** of dependency files/repository between different `build`s.

For example, if there are multiple contributors for your content, and there is a CI process to check everyone's change before merging their PRs, you truly want everyone including CI to use the same versions of the dependency files to builds, to provide valid checking result and reduce influences of different versions of dependencies.

Docfx uses `.lock.json`. Whenever you run docfx `restore`, docfx generates or updates your dependency lock. which will look something like this:

```json
{
    "git":{
        "https://github.com/docfx-code-sample#master":{
            "version": "fd6968dec9a9a39aec1845232466fe35fca520da"
        },
        "https://github.com/docfx-code-sample#live":{
            "version": "a82cb2a51f6925b2fa87275218b01678c5aadfe1",
            "git":{
                "https://github.com/azure-code-sample#live":{
                    "version": "c9a9a39aec184523246678c5aadfea82cb2"
                }
            },
            "downloads":{
                "https:///some-blob-service/build-history.json":{
                    "version": "66458170ba048c7fc2de4+%18b265bc4e9a6b4fdff8b5210caf9e357%22"
                }
            }
        }
    },
    "downloads":{
        "https:///some-blob-service/github-user-cache.json":{
            "version": "18b265bc4e9a6bf4cb2d+%22f02fde5b4f3817266458170ba048c7fc2de46287%22"
        }
    }
}
```

This file describes an `exact`, and more importantly `reproducible` `dependencies` trees. Once it's present, any future `restore` will base its work off this file, instead of recalculating`.

The presence of the `dependency lock` changes the `restore` behavior:

- The dependency files/repository described by this `dependency lock` is reproduced, which means reproducing the structure/version described in the file.
- Any missing version or dependencies are restored in the usual fashion.