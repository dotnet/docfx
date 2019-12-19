# Dependency Repository

Docfx support to build repo content which has `dependencies` to another repository. The `dependencies` relationships include

  - Inclusion (token/code-snippet files in another repository)
  - Content (articles/resource files in another repository)

## How to config

Define the dependent repositories in `docfx.yml` `dependencies` configuration with alias:

Example:

```yml
dependencies:
  _csharplang: "https://github.com/dotnet/csharplang#master"
```
Or

```yml
dependencies:
  _csharplang: 
    url: https://github.com/dotnet/csharplang
    branch: master
```

Above two configurations are identical:
  - The key `_csharplang` is the `dependency alias`, we will talk about it later detailed.
  - The **value** represents the dependency repository information, including `remote url`, `branch` and others.

## How to use

### Restore before Build

Run `docfx restore` to restore the dependency repositories to local before using them, you can find the details from [restore-file-and-git](./restore-file-and-git.md#restore-dependency-repositories)

### To include token/codesnippet file in dependency repositories

Token and code-snippet files will be built inside the page which includes them, you need Specify the `dependency alias` at the beginning of the file path like below:

Example: 

```md
[!code-csharp[Main](_csharplang/program.cs)]
```

In above example, the `_csharplang` is the `dependency alias`, which means that this file is from `https://github.com/dotnet/csharplang` repository, file path is `program.cs`.

### To build files in dependency repositories

The files which to be built to page can also be from dependency repository, which means that you can build/publish **the mixed the content in the repository and other repositories together**.

> Note: The dependency repository may be also referenced by others, which means that when building/publishing some files from dependency repository, these published pages may be duplicated with other sites.

- Turn on the `includeInBuild` flags in the `Dependency` config

    ```yml
    dependencies:
        _csharplang: 
            url: https://github.com/dotnet/csharplang
            branch: master
            includeInBuild: true
    ```

- Specify the `dependency alias` which you want to include in the `build scope`

    ```yml
    file:
        _csharplang/**/*.md
    excludes:
        _csharplang/**/include/**/*
    ```

- Specify the `dependency alias` in the `routes` to route the built/published pages

    ```yml
    routes:
        _csharplang/: docs/csharplang/
    ```

- Finally we get:

    ```yml
    file:
        _csharplang/**/*.md
    excludes:
        _csharplang/**/include/**/*
    routes:
        _csharplang/: docs/csharplang/
    dependencies:
        _csharplang: 
            url: https://github.com/dotnet/csharplang
            branch: master
            includeInBuild: true
    ```

In above example, it includes **all files** except `includes` folder of `https://github.com/dotnet/csharplang` repository in its build scope, and all these files will be built and published to `docs/csharplang` folder.

> Note: The default value of "includeInBuild" is false

> Note: If the `includeInBuild` flag of dependency repository is not turned on explicitly, all files will not be build/published even the `dependency alias` is used, and also links to files/resources in these dependency repositories will be treated as warnings.
