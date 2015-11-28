Version Notes (Current Version: v1.1)
=======================================
v1.1
-------------
1. Rewrite Glob
    The syntax of glob is:
    1. `**` is called globstar, it matches any number of characters, including `/`, as long as it's the only thing in a path part. 
    2. If `**` is right behind `/`, it is a shortcut for `**/*`.
    3. `*` matches any number of characters, but not `/`
    4. `?` matches 1 characters, but not `/`
    5. `{}` allows for a comma-separated list of "or" expressions, e.g. `{a,b}` => `a` and `b`
    6. `!` at the beginning of a pattern will negate the match
    7. `[...]` matches a range of characters, similar to a RegExp range 
    8. `/` is considered as path separator, while `\` is considered as escape character
2. Support `fileMetadata`. You can specify different metadata value using glob pattern
3. Improve overwrite functionality.
    Now you can overwrite not only summary/remarks, but also descriptions for parameters. You can even add exceptions.
4. Now the latest project.json projects are also supported in DNX version.
5. Simple code snippet is now supported, syntax is `[!code-REST-i[title](path "optionalTitle")]`
6. Url is now encoded in markdown link.

v1.0
-------------
1. Add section syntax in DFM
2. Fix several bugs in DFM
3. Update default template: rename css/js file
4. Fix several display issue in default template

v0.3
-------------
1. Support Static Website Templates
2. Schema change to docfx.json