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

The restored files will be stored at `%DOCFX_APPDATA_PATH%/downloads/{url-short-path}/content` like:

```text
`%DOCFX_APPDATA_PATH%`
    | - downloads
        | - raw.gith..tent.com+docascode+docfx-..+62b0448+extend1.yml+dc363b0e
            | - content

```

And there is another etag file `etag` under `%DOCFX_APPDATA_PATH%/downloads/{url-short-path}/` tracking the `etag` info of this file if has.

```text
`%DOCFX_APPDATA_PATH%`
    | - downloads
        | - raw.gith..tent.com+docascode+docfx-..+62b0448+extend1.yml+dc363b0e
            | - content
            | - etag

```

`etag`:
```text
"f8b4e180558bb672ba084a0baa2c345c642328e4"
```

- `{url-short-path}` is calculated from `file` url.
- `{etag}` is timestamp or something else supported by the service which stores the `file`.

Same url will always be restored to the same place, and we are using `Process Lock` to read/write the file and its etag file.

> NOTE: docfx will load all restored files' content at the first stage of build, never load again, in case this file is restored again(changed) in current build.

If the `etag` is supported by its service, docfx avoids duplicated downloads for the file never changed.

## Restore dependency repositories

There are some cases that the docfx `build` depends on external repositories, which includes `tokens`, `code-snippets` or other resources/content.

For example, some files are referencing `code example` for better explanations, but some of the code examples stores in another repository, maintained by another team/owners, with the dependency repository, you have the ability to reference external `code example` to be built in your page.

[Config](config.md):

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
        "date": "{date}"
    },
    {
        "id": 2,
        "branch": "{branch}",
        "commit": "{commit}",
        "date": "{date}"
    }
]
```

- `{url-short-path}` is calculated from `git remote` url
- `{branch}` is the branch name
- `{commit}` is the HEAD commit
- `{date}` is the last access date, it will be set or overwritten during restore.

We are using [Shared/Exclusive Lock](https://www.ibm.com/support/knowledgecenter/en/SSGMCP_5.1.0/com.ibm.cics.ts.applicationprogramming.doc/topics/dfhp39o.html) to tracking each index using states:
- During build, we acquire an index available from pool with `shared lock`.
- During restore, we acquire an index available from pool with `exclusive lock`.

>NOTE: Shared and Exclusive Lock need to be cross process, we are not going to discuss about that details here
