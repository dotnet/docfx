// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
var path = require('path');
var gulp = require('gulp');
var minify = require('gulp-minify-css');
var rename = require('gulp-rename');
var concat = require('gulp-concat');
var copy = require('gulp-copy');

var vendor = {
  css: [
        'bower_components/highlightjs/styles/github-gist.css'
       ],
  js: [
       'bower_components/highlightjs/highlight.pack.min.js'
      ]
  };

gulp.task('concat', function () {
  gulp.src(vendor.css)
    .pipe(minify({keepBreaks: true}))
    .pipe(rename({
        suffix: '.min'
    }))
    .pipe(concat('docfx.vendor.css'))
    .pipe(gulp.dest('./styles/'))
  ;
  gulp.src(vendor.js)
    .pipe(concat('docfx.vendor.js'))
    .pipe(gulp.dest('./styles/'))
  ;
});

gulp.task('default', ['concat']);
