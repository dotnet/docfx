var path = require('path');
var gulp = require('gulp');
var minify = require('gulp-minify-css');
var rename = require('gulp-rename');
var concat = require('gulp-concat');
var copy = require('gulp-copy');

var vendor = {
  css: ['bower_components/bootstrap/dist/css/bootstrap.css', 'bower_components/highlightjs/styles/solarized_dark.css'],
  js: ['bower_components/jquery/dist/jquery.min.js', 'bower_components/bootstrap/dist/js/bootstrap.min.js', 'bower_components/highlightjs/highlight.pack.min.js'],
  font: {
    src: ['*'],
    cwd: 'bower_components/bootstrap/dist/fonts/'
  }
}

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

gulp.task('copy', function () {
  gulp.src(vendor.font.src, {cwd: vendor.font.cwd})
    .pipe(copy('./fonts/'))
  ;
});

gulp.task('default', ['concat', 'copy']);
