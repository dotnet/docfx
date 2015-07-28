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
    },
    header:{
      vb:{
        src: "**/*.vb",
        cwd: "..",
        expand: true,
        options: {
          content: "' Copyright (c) Microsoft. All rights reserved.\n\
' Licensed under the MIT license. See LICENSE file in the project root for full license information.\n\n"
        }
      },
      cs:{
        src: "**/*.cs",
        cwd: "..",
        expand: true,
        options: {
          content: "// Copyright (c) Microsoft. All rights reserved.\n\
// Licensed under the MIT license. See LICENSE file in the project root for full license information.\n\n"
        }
      },
      js:{
        src: ["**/*.js", "!src/lunr.min.js"],
        cwd: "../src/docfx.website.themes/default/app",
        expand: true,
        options: {
          content: "// Copyright (c) Microsoft. All rights reserved.\
 Licensed under the MIT license. See LICENSE file in the project root for full license information.\n"
        }
      }
    }
  });


  grunt.config.set('conf', conf);
  grunt.registerMultiTask('header', function(){
    var header = this.options().content;
    this.files.forEach(function(filePair){
      filePair.src.forEach(function(src){
        if (grunt.file.isFile(src)){
          var content = grunt.file.read(src);
          if (content.indexOf(header) < 0) {
            grunt.verbose.writeln('Adding header to ' + src);
            content = header +  content;
            grunt.file.write(src, content);
          }
          else {
            grunt.verbose.writeln('Header already exists in ' + src + ', skipped.');
          }
        }
      })
    })
  });
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