// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
var path = require('path');
var gulp = require('gulp');
var minifyCss = require('gulp-minify-css');
var minifyJs = require('gulp-minify');
var rename = require('gulp-rename');
var concat = require('gulp-concat');
var copy = require('gulp-copy');

var vendor = {
  css: ['bower_components/bootstrap/dist/css/bootstrap.css',
    'bower_components/highlightjs/styles/github-gist.css'
  ],
  js: ['bower_components/jquery/dist/jquery.min.js',
    'bower_components/bootstrap/dist/js/bootstrap.min.js',
    'bower_components/highlightjs/highlight.pack.min.js',
    'bower_components/js-url/url.min.js',
    'bower_components/twbs-pagination/jquery.twbsPagination.min.js',
    "bower_components/mark.js/dist/jquery.mark.min.js",
    "bower_components/anchor-js/anchor.min.js"
  ],
  webWorker: {
    src: ['lunr.js'],
    cwd: 'bower_components/lunr.js/'
  },
  font: {
    src: ['*'],
    cwd: 'bower_components/bootstrap/dist/fonts/'
  }
}

gulp.task('concat:css', function () {
  return gulp.src(vendor.css)
    .pipe(minifyCss({ keepBreaks: true }))
    .pipe(rename({ suffix: '.min' }))
    .pipe(concat('docfx.vendor.css'))
    .pipe(gulp.dest('./styles/'));
})

gulp.task('concat:js', function () {
  return gulp.src(vendor.js)
    .pipe(concat('docfx.vendor.js'))
    .pipe(gulp.dest('./styles/'))
    ;
});

gulp.task('concat', gulp.parallel('concat:css', 'concat:js'));

gulp.task('copy:font', function () {
  return gulp.src(vendor.font.src, { cwd: vendor.font.cwd })
    .pipe(copy('./fonts/'));
});

gulp.task('copy:lunr', function () {
  return gulp.src('bower_components/lunr.js/lunr.js')
    .pipe(minifyJs({ ext: { min: '.min.js' } }))
    .pipe(gulp.dest('./styles/'));
});

gulp.task('copy', gulp.parallel('copy:font', 'copy:lunr'));

gulp.task('default', gulp.parallel('concat', 'copy'));
