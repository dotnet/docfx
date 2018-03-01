var path = require('path');

module.exports = function(grunt) {
  'use strict';
  var configKey = 'Configuration';
  var conf = grunt.option(configKey) || process.env[configKey] || 'Debug';

  // Generate version
  var suffix = getDateTime();
  var versionFromOption = grunt.option("uv");
  if (versionFromOption){
    var index = versionFromOption.indexOf('v');
    if ( index > -1) {
      versionFromOption = versionFromOption.substr(index + 1);
    }
  }
  console.log("Use version from option: " + versionFromOption);
  var nugetPackageVersion = versionFromOption || ('0.1.0-alpha-' + suffix);

  // load all grunt tasks matching the `grunt-*` pattern
  require('load-grunt-tasks')(grunt);
  grunt.initConfig({
    nugetpack: {
      'docfx.msbuild': {
        src: docfxSrc + "/docfx.msbuild.nuspec",
        dest: docfxDest,
        options: {
          version: nugetPackageVersion
        }
      },
      'msdn.4.5.2': {
        src: '../src/nuspec/msdn.4.5.2/msdn.4.5.2.nuspec',
        dest: '../artifacts/msdn.4.5.2/' + conf,
        options: {
          version: nugetPackageVersion
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
        src: ["**/*.js", "!**/*.min.js", "!**/*.vendor.js"],
        cwd: "../src/docfx.website.themes/",
        expand: true,
        options: {
          content: "// Copyright (c) Microsoft. All rights reserved.\
 Licensed under the MIT license. See LICENSE file in the project root for full license information.\n"
        }
      },
      html:{
        src: ["**/*.html"],
        cwd: "../src/docfx.website.themes/",
        expand: true,
        options: {
          content: "<!-- Copyright (c) Microsoft Corporation. All Rights Reserved.\
 Licensed under the MIT License. See License.txt in the project root for license information. -->\n"
        }
      },
      css:{
        src: "**/*.less",
        cwd: "../src/docfx.website.themes/",
        expand: true,
        options: {
          content: "/* Copyright (c) Microsoft Corporation. All Rights Reserved.\
 Licensed under the MIT License. See License.txt in the project root for license information. */\n"
        }
      }
    },
  updateVersion: {
    // All project.json
    projectJson : { src:['../src/**/project.json', '../tools/**/project.json', '../plugins/**/project.json'], options: {
      type: 'json'
    }}
  }
  });

  grunt.config.set('conf', conf);
  grunt.registerMultiTask('updateVersion', function(){
    var type = this.options().type;
    var version = versionFromOption;
    if (!version) {
      return grunt.util.error("version must be provided by --uv=<version>!");
    }

    if (type === 'json'){
      var projects = {};
      this.files.forEach(function(filePair){
        filePair.src.forEach(function(src){
          if (grunt.file.isFile(src)){
            // Get DNX project name
            var project = path.basename(path.dirname(src));
            projects[project] = src;
          }
        });
        for (var key in projects) {
          if (projects.hasOwnProperty(key)) {
            var src = projects[key];
            var json = grunt.file.readJSON(src);
            if (json.version !== version){
              json.version = version;
              var deps = json.dependencies;
              if (deps) {
                for (var key in deps) {
                  if (deps.hasOwnProperty(key) && projects.hasOwnProperty(key)) {
                    deps[key] = version;
                  }
                }
              }
              grunt.file.write(src, JSON.stringify(json, null, 2), {encoding: "UTF8"});
            }
          }
        }
      })
    }
  });
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

  // disable docfx.msbuild nuget package generation here as it is moved to docfx.msbuild.csproj
  grunt.registerTask('pack', ['nugetpack:msdn.4.5.2']);
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