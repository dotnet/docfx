module.exports = function(grunt) {
  'use strict';
  var configKey = 'Configuration';
  var conf = grunt.option(configKey) || process.env[configKey] || 'Debug';
  var docfxSrc = "../target/" + conf + "/docfx.msbuild.nuspec";
  var docfxDest = "../artifacts/docfx.msbuild/" + conf;

  // Generate version
  var suffix = getDateTime();
  var nugetPackageVersion = grunt.option("uv") || ('0.1.0-alpha-' + suffix);

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
        src: ["**/*.js", "!src/lunr.min.js"],
        cwd: "../src/docfx.website.themes/default/app",
        expand: true,
        options: {
          content: "// Copyright (c) Microsoft. All rights reserved.\
 Licensed under the MIT license. See LICENSE file in the project root for full license information.\n"
        }
      },
      html:{
        src: ["**/*.html"],
        cwd: "../src/docfx.website.themes/default/app",
        expand: true,
        options: {
          content: "<!-- Copyright (c) Microsoft Corporation. All Rights Reserved.\
 Licensed under the MIT License. See License.txt in the project root for license information. -->\n"
        }
      },
      css:{
        src: "**/*.less",
        cwd: "../src/docfx.website.themes/default/app",
        expand: true,
        options: {
          content: "/* Copyright (c) Microsoft Corporation. All Rights Reserved.\
 Licensed under the MIT License. See License.txt in the project root for license information. */\n"
        }
      }
    },
  updateVersion: {
    // All project.json
    // docfx.msbuild.csproj
    projectJson : { src:'../src/**/project.json', options: {
      type: 'json'
    }},
    md: {src: '../RELEASENOTE.md', options: {
      type: 'md'
    }}
  }
  });


  grunt.config.set('conf', conf);
  grunt.registerMultiTask('updateVersion', function(){
    var type = this.options().type;
    var version = grunt.option("uv");
    if (type === 'json'){
      this.files.forEach(function(filePair){
        filePair.src.forEach(function(src){
          if (grunt.file.isFile(src)){
            var json = grunt.file.readJSON(src);
            if (json.version !== version){
              json.version = version;
              grunt.file.write(src, JSON.stringify(json, null, 2), {encoding: "UTF8"});
            }
          }
        })
      })
    }
    else if (type === 'md'){
      var versionLine = "Current Version: " + version;
      this.files.forEach(function(filePair){
        filePair.src.forEach(function(src){
          if (grunt.file.isFile(src)){
            var md = grunt.file.read(src);
            var lines = md.split('\n');
            if (lines.length > 0) {
              lines[0] = versionLine;
            } else {
              lines.push(versionLine);
            }
            grunt.file.write(src, lines.join('\n'), {encoding: "UTF8"});
          }
        })
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