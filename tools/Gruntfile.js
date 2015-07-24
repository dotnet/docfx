module.exports = function(grunt) {
  'use strict';
  var configKey = 'Configuration';
  var conf = grunt.option(configKey) || process.env[configKey] || 'Debug';
  var docfxSrc = "../target/" + conf + "/docfx.msbuild.nuspec";
  var docfxDest = "../artifacts/docfx.msbuild/" + conf;

  // Generate version
  var suffix = getDateTime();
  var version = (grunt.option("version") || '0.1.0-alpha') + "-" + suffix;

  // load all grunt tasks matching the `grunt-*` pattern
  require('load-grunt-tasks')(grunt);
  grunt.initConfig({
    copy: {
      'docfx.msbuild': {
        files: [{
          expand: true,
          src: ['*.dll', '*docfx.exe*'],
          cwd: "../target/" + conf + "/docfx",
          dest: docfxSrc + "/tools",
          nonull: true,
        }, {
          expand: true,
          src: ['**'],
          cwd: '../src/nuspec/docfx.msbuild',
          dest: docfxSrc,
        }]
      },
    },
    nugetpack: {
      'docfx.msbuild': {
        src: docfxSrc + "/docfx.msbuild.nuspec",
        dest: docfxDest,
        options: {
          version: version
        }
      },
      'msdn.4.5.2': {
        src: '../src/nuspec/msdn.4.5.2/msdn.4.5.2.nuspec',
        dest: '../artifacts/msdn.4.5.2/' + conf,
        options: {
          version: version
        }
      }
    }
  });


  grunt.config.set('conf', conf);
  grunt.registerTask('pack', ['copy', 'nugetpack']);
  grunt.registerTask('default', ['pack']);

  function getDateTime() {
    var date = new Date();
    var hour = date.getHours();
    var hourString = (hour < 10 ? "0" : "") + hour;
    var min = date.getMinutes();
    var minString = (min < 10 ? "0" : "") + min;
    var year = date.getFullYear().toString().substring(2);
    var month = date.getMonth() + 1;
    var monthString = (month < 10 ? "0" : "") + month;
    var day = date.getDate();
    var dayString = (day < 10 ? "0" : "") + day;
    return year + monthString + dayString + hourString + minString;
  }
};