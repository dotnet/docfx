var path = require('path');
var gulp = require('gulp');
var zip = require('gulp-zip');
var minimist = require('minimist');
var uniqueFiles = require('gulp-unique-files');
var streamqueue = require('streamqueue');

var knownOptions = {
    string: 'dir',
    default: { dir: '../docfx/Template/', t: '' }
};

var options = minimist(process.argv.slice(2), knownOptions);
var files = [
    "fonts/*",
    "partials/*",
    "styles/*",
    "*.js",
    "*.tmpl",
    "favicon.ico",
    "logo.svg",
    "global.json"];

var pack = {
    "default": [
        {
            "files": files,
        }
    ],
    "default(zh-cn)": [
        {
            "files": files,
        }
    ],
    "iframe.html": [
        {
            "files": files,
            "cwd": "default",
        },
        {
            "files": files, // Overrides the former one if file name is the same
        }
    ],
    "msdn.html": [
        {
            "files": [
                "ManagedReference.html.primary.js",
                "partials/classSubtitle.tmpl.partial",
                "partials/namespaceSubtitle.tmpl.partial",
            ],
            "cwd": "default",
        },
        {
            "files": [
                "partials/title.tmpl.partial",
                "partials/namespace.tmpl.partial",
                "global.json",
            ],
            "cwd": "op.html",
        },
        {
            "files": files
        }
    ],
    "op.html": [
        {
            "files": [
                "ManagedReference.html.primary.js",
                "partials/classSubtitle.tmpl.partial",
                "partials/namespaceSubtitle.tmpl.partial",
            ],
            "cwd": "default",
        },
        {
            "files": [
                "ManagedReference.mta.json.tmpl",
                "conceptual.mta.json.tmpl",
            ],
            "cwd": "docs.html",
        },
        {
            "files": files
        }
    ],
     "op(zh-cn).html": [
        {
            "files": "partials/title.tmpl.partial",
            "cwd": "default(zh-cn)"
        },
        {
            "files": files,
        }
    ],
    "op(ar-SA).html": [
        {
            "files": "partials/title.tmpl.partial",
            "cwd": "op.html"
        },
        {
            "files": files,
        }
    ],
    "vs.html": [
        {
            "files": [
                "ManagedReference.html.primary.js",
                "partials/classSubtitle.tmpl.partial",
                "partials/namespaceSubtitle.tmpl.partial",
            ],
            "cwd": "default",
        },
        {
            "files": [
                "partials/title.tmpl.partial",
                "partials/namespace.tmpl.partial",
                "global.json",
            ],
            "cwd": "op.html",
        },
        {
            "files": files,
            "cwd": "msdn.html",
        },
        {
            "files": files
        }
    ],
    "docs.html": [
         {
            "files": [
                "ManagedReference.html.primary.js",
                "partials/classSubtitle.tmpl.partial",
                "partials/namespaceSubtitle.tmpl.partial",
            ],
            "cwd": "default",
        },
        {
            "files": [
                "global.json",
            ],
            "cwd": "op.html",
        },
        {
            "files": files
        }
    ]
};

gulp.task('help', function () {
    console.log("Usage:");
    console.log("gulp pack [-t <templateName>] [-dir <outputFolder>]");
});

gulp.task('pack', function () {
    var dirname = options.dir;
    if (options.t) {
        var name = options.t;
        if (name) {
            if (pack.hasOwnProperty(name)) {
                packFiles(name, dirname);
            }
            else {
                console.error("No folder with name " + name + " is found.");
            }
        }
    } else {
        for (var key in pack) {
            if (pack.hasOwnProperty(key)) {
                packFiles(key, dirname);
            }
        }
    }

    function packFiles(key, dirname) {
        var files = pack[key];
        var filename = key + ".zip";
        if (!files || files.length === 0) {
            console.warn("no files to zip for " + key);
            return;
        }

        console.log("Pack files to " + dirname + filename);
        var streams = files.map(function (file) {
            return getStream(file, key);
        });
        var stream = streamqueue.apply(this, [{ objectMode: true }].concat(streams));
        stream
            .pipe(uniqueFiles())
            .pipe(zip(filename))
            .pipe(gulp.dest(dirname))
        ;
    }

    function getStream(file, key) {
        var cwd = file.cwd || key;
        var base = file.base || cwd;
        return gulp.src(file.files, { base: base, cwd: cwd });
    }
});

gulp.task('default', ['pack']);
