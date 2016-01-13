var path = require('path');
var gulp = require('gulp');
var zip = require('gulp-zip');
var minimist = require('minimist');

var knownOptions = {
  string: 'dir',
  default: { dir : '../docfx/Template/' }
};
var options = minimist(process.argv.slice(2), knownOptions);

var pack = {
  "default": [
    "fonts/*",
    "partials/*",
    "styles/*",
    "*.js",
    "*.tmpl",
    "favicon.ico",
    "logo.svg"
  ],
  "iframe.html": [
    "fonts/*",
    "partials/*",
    "styles/*",
    "*.js",
    "*.tmpl",
    "favicon.ico",
    "logo.svg"
  ],
  "msdn.html": [
    "partials/*",
    "*.js",
    "*.tmpl"
  ],
  "op.html": [
    "partials/*",
    "*.js",
    "*.tmpl"
  ],
  "vs.html": [
    "partials/*",
    "*.js",
    "*.tmpl"
  ],
  "docs.html": [
    "partials/*",
    "*.js",
    "*.tmpl"
  ]
};

gulp.task('pack', function () {
  var dirname = options.dir;
  for (var key in pack) {
    if (pack.hasOwnProperty(key)) {
      var files = pack[key];
      var filename = key + ".zip";
      console.log("Pack files to " + dirname + filename);
      gulp.src(files, { base: key, cwd: key })
        .pipe(zip(filename))
        .pipe(gulp.dest(dirname))
      ;
    }
  }
});

gulp.task('default', ['pack']);
