var path = require('path');
var gulp = require('gulp');
var minify = require('gulp-minify-css');
var rename = require('gulp-rename');
var concat = require('gulp-concat');
var copy = require('gulp-copy');
var zip = require('gulp-zip');
var minimist = require('minimist');

var knownOptions = {
  string: 'dir',
  default: { dir : '../../docfx/Template/' }
};
var options = minimist(process.argv.slice(2), knownOptions);
var filename = path.basename(process.cwd()) + ".zip";

var vendor = {
  css: ['bower_components/bootstrap/dist/css/bootstrap.css', 'bower_components/highlightjs/styles/solarized_dark.css'],
  js: ['bower_components/jquery/dist/jquery.min.js', 'bower_components/bootstrap/dist/js/bootstrap.min.js', 'bower_components/highlightjs/highlight.pack.min.js'],
  font: {
    src: ['*'],
    cwd: 'bower_components/bootstrap/dist/fonts/'
  }
}

var pack = [
  "fonts/*",
  "partials/*",
  "styles/*",
  "*.js",
  "*.tmpl",
  "favicon.ico",
  "logo.svg"
];

gulp.task('concat', function () {
  gulp.src(vendor.css)
    .pipe(minify({keepBreaks: true}))
    .pipe(rename({
        suffix: '.min'
    }))
    .pipe(concat('vendor.css'))
    .pipe(gulp.dest('./styles/'))
  ;
  gulp.src(vendor.js)
    .pipe(concat('vendor.js'))
    .pipe(gulp.dest('./styles/'))
  ;
});

gulp.task('copy', function () {
  gulp.src(vendor.font.src, {cwd: vendor.font.cwd})
    .pipe(copy('./fonts/'))
  ;
});

gulp.task('pack', function () {
  var dirname = options.dir;
  console.log("Pack files to " + dirname + filename);
  gulp.src(pack, {base: "."})
    .pipe(zip(filename))
    .pipe(gulp.dest(dirname))
  ;
});

gulp.task('default', ['concat', 'copy']);
