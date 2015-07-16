// Developed upon generator-angular 0.11.1

// # Globbing
// for performance reasons we're only matching one level down:
// 'test/spec/{,*/}*.js'
// use this if you want to recursively match all subfolders:
// 'test/spec/**/*.js'

module.exports = function (grunt) {
  'use strict';
  var path = require('path');
  
  // Load grunt tasks automatically
  require('load-grunt-tasks')(grunt);

  // Time how long tasks take. Can help when optimizing build times
  require('time-grunt')(grunt);

  // Configurable paths for the application
  var appConfig = {
    app: require('./bower.json').appPath || 'app',
    dist: 'dist'
  };

  // Define the configuration for all the tasks
  grunt.initConfig({

    // Project settings
    docfx: appConfig,

    // Watches files for changes and runs tasks based on the changed files
    watch: {
      bower: {
        files: ['bower.json'],
        tasks: ['wiredep']
      },
      js: {
        files: ['<%= docfx.app %>/src/{,*/}*.js'],
        tasks: ['newer:jshint:all'],
        options: {
          livereload: '<%= connect.options.livereload %>'
        }
      },
      jsTest: {
        files: ['<%= docfx.app %>/tests/{,*/}*.js'],
        tasks: ['newer:jshint:test', 'karma']
      },
      styles: {
        files: ['<%= docfx.app %>/content/css/{,*/}*.{css,less}'],
        tasks: ['less', 'newer:copy:styles', 'autoprefixer']
      },
      gruntfile: {
        files: ['Gruntfile.js']
      },
      livereload: {
        options: {
          livereload: '<%= connect.options.livereload %>'
        },
        files: [
          '<%= docfx.app %>/{,*/}*.html',
          '.tmp/styles/{,*/}*.css',
        ]
      }
    },

    // The actual grunt server settings
    connect: {
      options: {
        port: 9000,
        // Change this to '0.0.0.0' to access the server from outside.
        hostname: 'localhost',
        livereload: 35729
      },
      livereload: {
        options: {
          open: true,
          middleware: function (connect) {
            return [
              connect.static('.tmp'),
              connect.static('app/content'),
              connect.static('sample'),
              connect().use(
                '/bower_components',
                connect.static('./bower_components')
              ),
              connect.static('app/src')
            ];
          }
        }
      },
      test: {
        options: {
          port: 9001,
          middleware: function (connect) {
            return [
              connect.static('.tmp'),
              connect.static('app/tests'),
              connect().use(
                '/bower_components',
                connect.static('./bower_components')
              ),
              connect.static('app/src')
            ];
          }
        }
      },
      dist: {
        options: {
          open: true,
          keepalive: true,
          middleware: function (connect) {
            return [
              connect.static('dist'),
              connect.static('sample')
            ];
          }
        }
      }
    },

    // Make sure code styles are up to par and there are no obvious mistakes
    jshint: {
      options: {
        jshintrc: '.jshintrc',
        reporter: require('jshint-stylish')
      },
      all: {
        src: [
          'Gruntfile.js',
          '<%= docfx.app %>/src/{,*/}*.js'
        ]
      },
      test: {
        options: {
          jshintrc: '<%= docfx.app %>/tests/.jshintrc'
        },
        src: ['<%= docfx.app %>/tests/{,*/}*.js']
      }
    },

    // Empties folders to start fresh
    clean: {
      dist: {
        files: [{
          dot: true,
          src: [
            '.tmp',
            '<%= docfx.dist %>/{,*/}*',
            '!<%= docfx.dist %>/.git{,*/}*'
          ]
        }]
      },
      server: '.tmp'
    },

    less: {
      dev: {
        options: {
          compress: false,
        },
        files: {
          '.tmp/styles/main.css': '<%= docfx.app %>/content/css/*.less',
        },
      }
    },

    // Add vendor prefixed styles
    autoprefixer: {
      options: {
        browsers: ['last 1 version']
      },
      server: {
        options: {
          map: true,
        },
        files: [{
          expand: true,
          cwd: '.tmp/styles/',
          src: '{,*/}*.css',
          dest: '.tmp/styles/'
        }]
      },
      dist: {
        files: [{
          expand: true,
          cwd: '.tmp/styles/',
          src: '{,*/}*.css',
          dest: '.tmp/styles/'
        }]
      }
    },

    // Automatically inject Bower components into the app
    wiredep: {
      app: {
        src: ['<%= docfx.app %>/src/index.html']
      },
      test: {
        devDependencies: true,
        src: '<%= karma.unit.configFile %>',
        fileTypes:{
          js: {
            block: /(([\s\t]*)\/{2}\s*?bower:\s*?(\S*))(\n|\r|.)*?(\/{2}\s*endbower)/gi,
              detect: {
                js: /'(.*\.js)'/gi
              },
              replace: {
                js: '\'{{filePath}}\','
              }
            }
          }
      }
    },

    // Reads HTML for usemin blocks to enable smart builds that automatically
    // concat, minify and revision files. Creates configurations in memory so
    // additional tasks can operate on them
    useminPrepare: {
      html: '<%= docfx.app %>/src/index.html',
      options: {
        dest: '<%= docfx.dist %>',
        flow: {
          html: {
            steps: {
              js: ['concat', 'uglifyjs'],
              css: ['cssmin']
            },
            post: {}
          }
        }
      }
    },

    // Performs rewrites based on the useminPrepare configuration
    usemin: {
      html: ['<%= docfx.dist %>/{,*/}*.html'],
    },

    htmlmin: {
      dist: {
        options: {
          collapseWhitespace: true,
          conservativeCollapse: true,
          collapseBooleanAttributes: true,
          removeCommentsFromCDATA: true,
          removeOptionalTags: true
        },
        files: [{
          expand: true,
          cwd: '<%= docfx.dist %>',
          src: ['*.html', 'views/{,*/}*.html'],
          dest: '<%= docfx.dist %>'
        }]
      }
    },

    // ng-annotate tries to make the code safe for minification automatically
    // by using the Angular long form for dependency injection.
    ngAnnotate: {
      dist: {
        files: [{
          expand: true,
          cwd: '.tmp/concat/scripts',
          src: '*.js',
          dest: '.tmp/concat/scripts'
        }]
      }
    },

    // Copies remaining files to places other tasks can use
    copy: {
      dist: {
        files: [{
          expand: true,
          dot: true,
          cwd: '<%= docfx.app %>/src',
          dest: '<%= docfx.dist %>',
          src: [
            '*.html',
            '{,*/}*.html'
          ]
        }, {
          expand: true,
          dot: true,
          cwd: '<%= docfx.app %>/content',
          dest: '<%= docfx.dist %>',
          src: [
            '*.{ico,png}'
          ]
        }, {
          expand: true,
          cwd: 'bower_components/bootstrap/dist',
          src: 'fonts/*',
          dest: '<%= docfx.dist %>'
        }]
      },
      styles: {
        expand: true,
        cwd: '<%= docfx.app %>/styles',
        dest: '.tmp/styles/',
        src: '{,*/}*.css'
      }
    },

    // Run some tasks in parallel to speed up the build process
    concurrent: {
      server: [
        'copy:styles'
      ],
      test: [
        'copy:styles'
      ],
      dist: [
        'copy:styles'
      ]
    },

    // zip to create the template package
    // template name is by default the current folder name
    // Sample usage: grunt compress --dist=../../docfx/Template
    compress: {
      dist: {
        options: {
          archive: path.join(grunt.option('dist')||'../../docfx/Template/', (grunt.option('name') || path.basename(process.cwd()) || 'default') + '.zip')
        },
        files: [
          {
            src: ['**'], 
            cwd: 'dist', 
            expand: true,
            dest: '.'
          }
        ]
      }
    },
    
    // Test settings
    karma: {
      unit: {
        configFile: 'karma.config.js',
        singleRun: true
      }
    }
  });


  grunt.registerTask('serve', 'Compile then start a connect web server', function (target) {
    if (target === 'dist') {
      return grunt.task.run(['build', 'connect:dist:keepalive']);
    }

    grunt.task.run([
      'clean:server',
      'wiredep',
      'less',
      'concurrent:server',
      'autoprefixer:server',
      'connect:livereload',
      'watch'
    ]);
  });

  grunt.registerTask('server', 'DEPRECATED TASK. Use the "serve" task instead', function (target) {
    grunt.log.warn('The `server` task has been deprecated. Use `grunt serve` to start a server.');
    grunt.task.run(['serve:' + target]);
  });

  grunt.registerTask('test', [
    'clean:server',
    'wiredep',
    'concurrent:test',
    'autoprefixer',
    'connect:test',
    'karma'
  ]);

  grunt.registerTask('build', [
    'clean:dist',
    'wiredep',
    'less',
    'useminPrepare',
    'concurrent:dist',
    'autoprefixer',
    'concat',
    'ngAnnotate',
    'copy:dist',
    'cssmin',
    'uglify',
    'usemin',
    'htmlmin'
  ]);

  grunt.registerTask('default', [
    'newer:jshint',
    'test',
    'build'
  ]);

  // TODO: use pack to generate zipped theme package for docfx
  grunt.registerTask('pack', ['build', 'compress']);
};
