// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
    "layout/*",
    "styles/*",
    "*.js",
    "*.tmpl",
    "*.liquid",
    "favicon.ico",
    "logo.svg",
    "token.json",
    "search-stopwords.json"];
var pack = {
    "common": [
        {
            "files": files,
        }
    ],
    "default": [
        {
            "files": files,
            "cwd": "common",
        },
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
            "cwd": "common",
        },
        {
            "files": files,
            "cwd": "default",
        },
        {
            "files": files, // Overrides the former one if file name is the same
        }
    ],
    "statictoc": [
        {
            "files": files,
            "cwd": "common",
        },
        {
            "files": files.concat("!toc.html.*"),
            "cwd": "default",
        },
        {
            "files": files, // Overrides the former one if file name is the same
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
