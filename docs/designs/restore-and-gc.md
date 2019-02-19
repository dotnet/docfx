# Restore

Docfx `restore` helps you to restore all you dependency repositories/files(defined in [config](config.md), fallback source repository and theme repository) into your local `%DOCFX_APPDATA_PATH%` for build preparation.

The default `%DOCFX_APPDATA_PATH%` is `%USERPROFILE%/.docfx`, but you can reset it to any other place.

The restore need make sure:

  - Once one version of file/repository is restored, this version of the file/repository should **NOT** be updated until be cleaned up by [GC](#Garbage collection). To make sure the `build`s which using this version are not broken causing by another `restore`.
  - Provide a mechanism to lock down the version of dependency file/repository for different `build`s, just like [npm package locks](https://docs.npmjs.com/files/package-locks)

## Restore files

There are some cases that the docfx `build` depends on external files for help. 

For example, if you want the docfx to resolve `gitHub contributors` from building file git commit history, the [GitHub User Cache](github-user-cache.md) file can be set to reduce the API calls to [GitHub](https://github.com). The file can be just a http(s) url, which `restore` will help to download for `build` usage.

```yml
github:
  userCache: https:///some-blob-service/github-user-cache.json
```

The restored files will be stored at `%DOCFX_APPDATA_PATH%/downloads/{url-short-path}/{number}` like:

```text
`%DOCFX_APPDATA_PATH%`
    | - downloads
        | - raw.gith..tent.com+docascode+docfx-te..ndencies+62b0448+extend1.yml+dc363b0e
            | - 1
            | - 2

```

And there is another index file `index.json` under `%DOCFX_APPDATA_PATH%/downloads/{url-short-path}/` tracking the detail info of each `{number} file`.

```text
`%DOCFX_APPDATA_PATH%`
    | - downloads
        | - raw.gith..tent.com+docascode+docfx-te..ndencies+62b0448+extend1.yml+dc363b0e
            | - 1
            | - 2
            | - index.json

```

`index.json`:
```json
[
    {
        "id": 1,
        "version": "{version}",
        "etag": "{etag}",
        "date": "{date}",
        "acquired": "{acquiring type}",
        "acquiredBy": [
            {
                "id": "1",
                "date": "{acquiring date}"
            }
        ]
    },
    {
        "id": 2,
        "version": "{version}",
        "etag": "{etag}",
        "date": "{date}",
        "acquired": "{acquiring type}",
        "acquiredBy": [
            {
                "id": "1",
                "date": "{acquiring date}"
            }
        ]
    }
]
```

- `{url-short-path}` is calculated from `file` url
- `{version}` is sha1 hash of file content, 
- `{etag}` is timestamp or something else supported by the service which stores the `file`.
- `{date}` is the download date, it will be set or overwritten during restore.
- `{acquired}` is tracking the file is in use or not(restoring or building), if it has value, this file can't be overwritten, otherwise, it can be overwritten for reusing during restore. `restoring` is exclusive lock, `building` is shared lock.
- `{acquiredBy}` is the info of acquirers
    - `{id}` is the acquirer id, usually it's the thread id
    - `{date}` is the acquiring date, we have a default period of requiring timeout, in case the docfx crashed.

If the `etag` is supported by its service, docfx avoids duplicated downloads for the file never changed.

## Restore dependency repositories

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

The restored dependency repositories will be stored at `%DOCFX_APPDATA_PATH%/git/{url-short-path}/{number}` organized by [git work tree](https://git-scm.com/docs/git-worktree) like:

```text
`%DOCFX_APPDATA_PATH%`
    | - git
        | - github.com+Microsoft+Luis-Samples+5e455995
            | - .git
            | - 1
            | - 2
```

And there is another file `index.json` under `%DOCFX_APPDATA_PATH%/git/{url-short-path}` tracking the detail info of each `work-tree`.

```text
`%DOCFX_APPDATA_PATH%`
    | - git
        | - github.com+Microsoft+Luis-Samples+5e455995
            | - .git
            | - 1
            | - 2
            | - index.json
```

`index.json`:
```json
[
    {
        "id": 1,
        "branch": "{branch}",
        "commit": "{commit}",
        "date": "{date}",
        "acquired": "{acquiring type}",
        "acquiredBy": [
            {
                "id": "1",
                "date": "{acquiring date}"
            }
        ]
    },
    {
        "id": 2,
        "branch": "{branch}",
        "commit": "{commit}",
        "date": "{date}",
        "acquired": "{acquiring type}",
        "acquiredBy": [
            {
                "id": "1",
                "date": "{acquiring date}"
            }
        ]
    }
]
```

- `{url-short-path}` is calculated from `git remote` url
- `{branch}` is the branch name
- `{commit}` is the HEAD commit
- `{date}` is the last restore date, it will be set or overwritten during restore.
- `{acquired}` is tracking the file is in use or not(restoring or building), if it has value, this work-tree can't be overwritten, otherwise, it can be overwritten for reusing during restore.`restoring` is exclusive lock, `building` is shared lock.
- `{acquiredBy}` is the info of acquirers
    - `{id}` is the acquirer id, usually it's the thread id
    - `{date}` is the acquiring date, we have a default period of requiring timeout, in case the docfx crashed.

## Dependency lock

There are some cases you want to keep using the **same version** of dependency files/repository between different `build`s.

For example, if there are multiple contributors for your content, and there is a CI process to check everyone's change before merging their PRs, you truly want everyone including CI to use the same versions of the dependency files/repositories for builds, to provide valid checking result and reduce influences of different versions of dependencies.

Docfx uses `.lock.json` as `dependency lock`. Whenever you run docfx `restore`, docfx generates or updates your `.lock.json`, which will look something like this:

```json
{
    "git":{
        "https://github.com/docfx-code-sample#master":{
            "commit": "fd6968dec9a9a39aec1845232466fe35fca520da"
        },
        "https://github.com/docfx-code-sample#live":{
            "commit": "a82cb2a51f6925b2fa87275218b01678c5aadfe1",
            "git":{
                "https://github.com/azure-code-sample#live":{
                    "commit": "c9a9a39aec184523246678c5aadfea82cb2"
                }
            },
            "downloads":{
                "https:///some-blob-service/build-history.json":{
                    "hash": "d9a9a39aec184523246678c5aadfea82cb2"
                }
            }
        }
    },
    "downloads":{
        "https:///some-blob-service/github-user-cache.json":{
            "hash": "e9a9a39aec184523246678c5aadfea82cb2"
        }
    }
}
```

- `git` is the version mappings of dependency repositories
- `downloads` is the version mappings of dependency files
- `commit` is `commit id` of the dependency repository 
- `hash` is the sha1 hash of file content

This file describes an `exact`, and more importantly `reproducible` `dependencies` trees. Once it's present, any future `restore` will base its work off this file, instead of recalculating`.

The presence of the `.lock.json` changes the `restore` behavior:

- The dependency files/repository described by this `.lock.json` is reproduced, which means reproducing the structure/version described in the file.
- Any **missing dependencies** are restored in the usual fashion, calculated from `docfx.yml`.

> Note: currently we only support `git` in `.lock.json` which means the `downloads` will be restored to latest version always, limited by the service provides these downloads.

> Note: The file is always sorted, so you can safely check-in this file to our content repository.

## Using locked dependencies

You can setup your `.lock.json` file(local file or a URL) in the configuration like

```yml
dependencyLock: https://some-blob-service/repo/branch/.lock.json
``` 

or 

```yml
dependencyLock: .lock.json
```

Whenever you run docfx `restore`, docfx will:

- [**GET**] **Try** to get the `.lock.json` file from configuration, 
  - if it's present, any future dependencies restore will base its work off this file.
  - if it's not set, restore the dependencies in the usual fashion
- [**UPDATE**] Once finished the restore, docfx will **try** to create/update the `.lock.json` file defined in configuration
  - If it's present, generate new/update existing `.lock.json` based on the input `.lock.json` + `docfx.yml`
  - If ir's not set, nothing happened

For example, run docfx `restore` with below `docfx.yml` and `.lock.json`

`docfx.yml`:
```yml
dependencies:
  docfx-sample-code: https://github.com/dotnet/docfx-sample-code#master
  azure-sample-code: https://github.com/microsoft/azure-sample-code#live
dependencyLock: .lock.json
```

`.lock.json`:
```json
{
    "git":{
        "https://github.com/dotnet/docfx-sample-code#master":{
            "commit": "19a9a39aec184523246678c5aadfea82cb2"
        }
    }
}
```

The output for `restored repositories` and `.lock.json` would be like:

`restored repositories`:
```text
`%DOCFX_APPDATA_PATH%`
    | - git
        | - github.com+dotnet+docfx-sample-code+5e455995
            | - .git
            | - master-19a9a39aec184523246678c5aadfea82cb2
        | - github.com+microsoft+azure-sample-code+5e4589s5
            | - .git
            | - master-29a9a39aec184523246678c5aadfea82cb2
```
`.lock.json`:
```json
{
    "git":{
        "https://github.com/dotnet/docfx-sample-code#master":{
            "commit": "19a9a39aec184523246678c5aadfea82cb2"
        },
        "https://github.com/microsoft/azure-sample-code#live":{
            "commit": "29a9a39aec184523246678c5aadfea82cb2"
        }
    }
}
```
For above example:

- Even there is new commits in `https://github.com/dotnet/docfx-sample-code` `master` branch, but with the `.lock.json` file, we still using the `19a9a39aec184523246678c5aadfea82cb2` commit.
- Since the `https://github.com/microsoft/azure-sample-code#live` is not defined in existing `.lock.json`, `restore` will check-out latest commit of live branch of this repository to one work tree and add it to `.lock.json`.

