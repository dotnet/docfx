(function() {
  'use strict';
   /*jshint validthis:true */
  function provider($q, $http, constants, urlService, tocCache, mdIndexCache) {
    function getYamlResponse(response){
      return jsyaml.load(response.data);
    }

    function valueHttpWrapper(value){
      var deferred = $q.defer();
      deferred.resolve(value);
      return deferred.promise;
    }

    this.valueHttpWrapper = valueHttpWrapper;
    this.getNavBar = function(){
      return $http.get(constants.TocFile)
      .then(getYamlResponse);
    };

    this.getContent = function(path){
      return $http.get(path)
      .then(getYamlResponse);
    };

    this.getMarkdownContent = function(path){
      function getContentComplete(response){
        return response.data;
      }
      return $http.get(path)
      .then(getContentComplete);
    };

    this.getTocContent = function(path) {
      if (!path) return valueHttpWrapper(null);
      var toc;
      path = urlService.normalizeUrl(path);
      var temp = tocCache.get(path);
      if (temp) {
        return valueHttpWrapper(temp);
      } else {
        toc = {
          path: path
        };
        return $http.get(path)
          .then(function(result) {
            var content = getYamlResponse(result);
            toc.content = content;
            tocCache.put(path, toc);
            return toc;
          }).catch(function(result) {
            tocCache.put(path, toc);
            return toc;
          });
      }
    };

    this.getMdContent = function(path) {
      if (!path) return valueHttpWrapper(null);
      var tempMdIndex;
      if (path) {
        tempMdIndex = mdIndexCache.get(path);
        if (tempMdIndex !== undefined) {
          return valueHttpWrapper(tempMdIndex);
        } else {
          return $http.get(path)
            .then(function(result) {
              // use json format for map file as be consistent with other map files, e.g. js.map, css.map.
              var content = result;
              mdIndexCache.put(path, content);
              return content;
            }).catch(function(result) {
              mdIndexCache.put(path, null);
              return valueHttpWrapper(null);
            });
        }
      } else {
        return valueHttpWrapper(null);
      }
    };

    this.getDefaultItem = function(array, action) {
      if (!action) return;
      if (array && array.length > 0) {
        return action(array[0]);
      }
    };
  }

  angular.module('docascode.contentService', ['docascode.constants', 'docascode.urlService'])
    // Post processing response not working as 404 in console is thrown out in xhr.send(post || null);
    .factory('myInterceptor', ['$q', function($q) {
      // Happenes after console 404
      // http://www.webdeveasy.com/interceptors-in-angularjs-and-useful-examples/
      return {
        response: function(rejection) {
          if (rejection.status === 404) {
            return $q.resolve();
          }

          return $q.reject(rejection);
        },
        responseError: function(rejection) {
          if (rejection.status === 404) {
            return $q.resolve();
          }

          return $q.reject(rejection);
        }
      };
    }])
    .config(['$httpProvider', function ($httpProvider) {
        // $httpProvider.interceptors.push('myInterceptor');
      }])
    .factory('tocCache', ['$cacheFactory', function($cacheFactory) {
      return $cacheFactory('toc-cache');
    }])
    .factory('mdIndexCache', ['$cacheFactory', function($cacheFactory) {
      return $cacheFactory('mdIndex-cache');
    }])
    .service('contentService', ['$q', '$http', 'docConstants', 'urlService', 'tocCache', 'mdIndexCache', provider]);

})();
(function() {
  'use strict';
  /*jshint validthis:true */
  function provider() {
    var player;
    function csplay(player, compileServiceBaseUrl) {
      if (typeof compileServiceBaseUrl === "undefined") {
        compileServiceBaseUrl = "";
      }
      if (compileServiceBaseUrl.substr(-1, 1) !== "/") {
        compileServiceBaseUrl += "/";
      }
      if (typeof player === "string") {
        player = document.getElementById(player);
      }
      // Create editor, split bar and output
      var top = document.createElement("div");
      var splitbar = document.createElement("div");
      var bottom = document.createElement("div");
      top.innerHTML = player.innerHTML;
      top.className = "csplay_editor";
      splitbar.className = "csplay_splitbar";
      bottom.className = "csplay_output";
      var cloned = player.cloneNode(false);
      player.parentNode.replaceChild(cloned, player);
      player = cloned;
      player.appendChild(top);
      player.appendChild(splitbar);
      player.appendChild(bottom);
      // Create ace editor
      var editor = ace.edit(top);
      editor.getSession().setMode("ace/mode/csharp");
      // Use jquery from here
      player = $(player);
      top = $(top);
      splitbar = $(splitbar);
      bottom = $(bottom);
      // Make splitbar draggable
      var dragging = false;
      splitbar.mousedown(function(e) {
        dragging = true;
      });
      player.mouseup(function() {
        dragging = false;
      }).mousemove(function(e) {
        if (dragging) {
          var topHeight = e.pageY - top.offset().top;
          var bottomHeight = top.outerHeight(true) + bottom.outerHeight(true) - topHeight;
          var splitbarHeight = splitbar.outerHeight(true);
          if (topHeight > 0 && bottomHeight > 0) {
            top.css("height", topHeight);
            bottom.css("height", "calc(100% - " + (topHeight + splitbarHeight) + "px)");
            editor.resize();
          }
          e.preventDefault();
        }
      });
      return {
        run: function(callback) {
          $.ajax({
            url: compileServiceBaseUrl + "run",
            type: "POST",
            data: editor.getValue(),
            contentType: "text/plain",
            success: function(data, status, xhr) {
              bottom.html(data).removeClass("error");
              if (typeof callback.success === "function") {
                callback.success(data, status, xhr);
              }
            },
            error: function(xhr, status, message) {
              if (typeof xhr.responseJSON.error_message === "string") {
                bottom.text(xhr.responseJSON.error_message).addClass("error");
              } else {
                bottom.text(xhr.responseText).addClass("error");
              }
              if (typeof callback.error === "function") {
                callback.error(xhr, status, message);
              }
            },
            complete: function(xhr, status) {
              if (typeof callback.complete === "function") {
                callback.complete(xhr, status);
              }
            }
          });
        },
        clearOutput: function() {
          bottom.html("");
        },
        editor: editor
      };
    }

    function createPlayer() {
      var player = csplay("player", "http://dotnetsandbox.azurewebsites.net" /* hardcode for now */ );
      player.editor.setTheme("ace/theme/ambiance");
      player.editor.setFontSize(16);
      $("#run").click(function() {
        var that = $(this);
        that.html('<i class="fa fa-refresh fa-fw fa-spin"></i>Run');
        that.addClass("disabled");
        player.run({
          complete: function() {
            that.text("Run");
            that.removeClass("disabled");
          }
        });
      });
      $("#close").click(function() {
        angular.element("#console").css("margin-left", "100%");
        if (player) player.editor.setReadOnly(true);
      });
      return player;
    }

    this.tryCode = function(enable, code) {
      if (enable) {
        if (typeof player === "undefined") {
          player = createPlayer();
        }
        player.editor.setValue(code, -1);
        player.editor.clearSelection();
        player.clearOutput();
        angular.element("#console").css("margin-left", "50%");
      } else {
        angular.element("#console").css("margin-left", "100%");
      }
      if (typeof player !== "undefined") {
        player.editor.setReadOnly(!enable);
      }
    };
  }


  angular.module('docascode.csplayService', ['docascode.constants', 'docascode.util'])
    .service('csplayService', ['$q', '$http', 'docConstants', 'docUtility', provider]);

})();
(function() {
  'use strict';
   /*jshint validthis:true */
  function provider($q, $http, docConstants, docUtility) {
    function normalizeUrl(url) {
      if (!url) return '';
      var arr = url.split(/[/|\\]/);
      var newArray = docUtility.cleanArray(arr);
      return newArray.join('/');
    }

    function urlAreEqual(thisUrl, thatUrl){
      return normalizeUrl(thisUrl) === normalizeUrl(thatUrl);
    }

    this.isAbsoluteUrl = function(url) {
      if (!url) return false;
      // general absolute url
      var r = new RegExp('^(?:[a-z]+:)?//', 'i');
      if (r.test(url)) return true;
      
      // If path already start with '#', consider it as absolute url
      if (url.indexOf('#') === 0) return true;
      return false;
    };

    this.normalizeUrl = normalizeUrl;
    this.urlAreEqual = urlAreEqual;
    
    this.getLinkHref = function(url, currentPath) {
      var pathInfo = this.getPathInfo(currentPath);
      return this.getHref(pathInfo.tocPath, pathInfo.contentPath, url);
    };
    
    this.getRemoteUrl = function(remote, startLine) {
      if (remote && remote.repo) {
        var repo = remote.repo;
        if (repo.substr(-4) === '.git') {
          repo = repo.substr(0, repo.length - 4);
        }
        var linenum = startLine ? startLine : 0;
        if (repo.match(/https:\/\/.*\.visualstudio\.com\/.*/g)) {
          // TODO: line not working for vso
          return repo + '#path=/' + remote.path;
        }
        if (repo.match(/https:\/\/.*github\.com\/.*/g)) {
          var path = repo + '/blob' + '/' + remote.branch + '/' + remote.path;
          if (linenum > 0) path += '/#L' + linenum;
          return path;
        }
      } else {
        return '';
      }
    };

    this.getPathInfo = function(currentPath) {
      if (!currentPath) return null;
      currentPath = normalizeUrl(currentPath);

      // seperate toc and content with !
      var index = currentPath.indexOf(docConstants.TocAndFileUrlSeperator);
      if (index < 0) {
        // If it ends with .md/.yml, render it without toc
        if ((docConstants.MdOrYamlRegexExp).test(currentPath)) {
          return {
            contentPath: currentPath
          };
        } else {
          return {
            tocPath: currentPath,
            tocFilePath: currentPath + '/' + docConstants.TocFile
          };
        }
      }

      return {
        tocPath: currentPath.substring(0, index),
        tocFilePath: currentPath.substring(0, index) + '/' + docConstants.TocFile,
        contentPath: currentPath.substring(index + 1, currentPath.length)
      };
    };

    this.getContentFilePath = function(pathInfo) {
      if (!pathInfo) return '';
      var path = pathInfo.tocPath ? pathInfo.tocPath + '/' : '';
      path += pathInfo.contentPath ? pathInfo.contentPath : docConstants.TocFile;
      return path;
    };

    this.getContentUrl = function(pathInfo) {
      if (!pathInfo) return pathInfo;
      var path = pathInfo.tocPath ? pathInfo.tocPath + docConstants.TocAndFileUrlSeperator : '';
      path += pathInfo.contentPath ? pathInfo.contentPath : docConstants.TocFile;
      return path;
    };

    this.getContentUrlWithTocAndContentUrl = function(tocPath, contentPath) {
      var path = tocPath ? tocPath + docConstants.TocAndFileUrlSeperator : '';
      path += contentPath ? contentPath : docConstants.TocFile;
      return path;
    };

    this.getPathInfoFromContentPath = function(navList, path) {
      // normalize path
      path = normalizeUrl(path);
      if (!navList || navList.length === 0) return {
        contentPath: path
      };

      for (var i = 0; i < navList.length; i++) {
        var href = navList[i].href;
        href = normalizeUrl(href) + '/'; // Append '/'' so that it must be a full path
        // return the first matched nav
        if (path.indexOf(href) === 0) {
          return {
            tocPath: href,
            tocFilePath: href + docConstants.TocFile,
            contentPath: path.replace(href, ''),
          };
        }
      }

      return {
        contentPath: path
      };
    };

    this.getAbsolutePath = function(currentUrl, relative) {
      if (!currentUrl) return relative;
      var pathInfo = this.getPathInfo(currentUrl);
      if (!pathInfo) return '';
      var current = this.getContentFilePath(pathInfo);
      var sep = '/',
        currentList = docUtility.cleanArray(current.split(sep)),
        relList = docUtility.cleanArray(relative.split(sep)),
        fileName = currentList.pop();

      var relPath = currentList;
      while (relList.length > 0) {
        var pathPart = relList.shift();
        if (pathPart === '..') {
          if (relPath.length > 0) {
            relPath.pop();
          } else {
            relPath.push('..');
          }
        } else {
          relPath.push(pathPart);
        }
      }

      return relPath.join(sep);
    };

    this.asyncFetchIndex = function(path, success, fail) {
      var deferred = $q.defer();

      //deferred.notify();
      var req = {
        method: 'GET',
        url: path,
        headers: {
          'Content-Type': 'text/plain'
        }
      };
      $http.get(req.url, req)
        .success(
          function(result) {
            if (success) success(result);
            deferred.resolve();

          }).error(
          function(result) {
            if (fail) fail(result);
            deferred.reject();
          }
        );

      return deferred.promise;
    };

    this.getTocContent = function($scope, path, tocCache) {
      if (path) {
        path = normalizeUrl(path);
        var temp = tocCache.get(path);
        if (temp) {
          $scope.toc = temp;
        } else {
          $scope.toc = {
            path: path
          };
          this.asyncFetchIndex(path, function(result) {
            var content = jsyaml.load(result);
            var toc = {
              path: path,
              content: content
            };
            tocCache.put(path, toc);
            $scope.toc = toc;
          }, function() {
            var toc = {
              path: path,
            };
            tocCache.put(path, toc);
            $scope.toc = toc;
          });
        }
      } else {
        $scope.toc = undefined;
      }
    };

    this.getMdContent = function($scope, path, mdIndexCache) {
      if (!path) return;
      var pathInfo = this.getPathInfo(path);
      var mdPath = normalizeUrl((pathInfo.tocPath || '') + '/' + docConstants.MdIndexFile);

      if (mdPath) {
        var tempMdIndex = mdIndexCache.get(mdPath);
        if (tempMdIndex) {
          if (tempMdIndex) $scope.mdIndex = tempMdIndex;
        } else {
          this.asyncFetchIndex(mdPath, function(result) {
            tempMdIndex = jsyaml.load(result);
            // This is the md file path
            mdIndexCache.put(mdPath, tempMdIndex);
            $scope.mdIndex = tempMdIndex;
          });
        }
      } else {
        $scope.toc = undefined;
      }
    };

    this.getDefaultItem = function(array, action) {
      if (!action) return;
      if (array && array.length > 0) {
        return action(array[0]);
      }
    };

    this.getHref = function(tocPath, sourcePageHref, targetPageHref) {
      if (!targetPageHref) return '';
      if (this.isAbsoluteUrl(targetPageHref)) return targetPageHref;
      // TODO: if path is : /#/toc1!../a.md => toc should be toc1/toc.yml?
       var path = this.getAbsolutePath(sourcePageHref, targetPageHref);

      return '#' + this.getContentUrl({tocPath:tocPath, contentPath:path});
    };
    
    // Href relative to current file
    this.getPageHref = function (currentPath, targetUrl, navItems) {
      if (!targetUrl) return '';
      if (this.isAbsoluteUrl(targetUrl)) return targetUrl;
      var pathInfo = this.getPathInfo(currentPath);
      var path = this.getAbsolutePath(currentPath, targetUrl);

      var toc = pathInfo.tocPath;
      if (navItems && angular.isArray(navItems)) {
        var matched = navItems.filter(function (s) {
          if (s.href) {
            var key = s.href + '/'; //append / to the href to make sure it is folder
            if (path.indexOf(key) === 0) return true;
          }
          return false;
        });
        if (matched.length > 0)
          {
            toc = matched[0].href;
            path = path.replace(toc, '');
          }
      }

      return '#' + this.getContentUrl({ tocPath: toc, contentPath: path });
    };
  }

  angular.module('docascode.urlService', ['docascode.constants', 'docascode.util'])
    .service('urlService', ['$q', '$http', 'docConstants', 'docUtility', provider]);

})();
/* global hljs */
/*
 * Define content load related functions used in docsController
 * Wrap Angular components in an Immediately Invoked Function Expression (IIFE)
 * to avoid variable collisions
 */

(function () {
  'use strict';
  /*jshint validthis:true */
  function provider($q, $http, $location, urlService, csplayService, contentService, docUtility) {
    this.transform = transform;
    this.updateHref = updateHref;
    var md = (function () {
      marked.setOptions({
        gfm: true,
        pedantic: false,
        sanitize: false, // DONOT sanitize html tags
      });

      var toHtml = function (markdown) {
        if (!markdown)
          return '';

        return marked(markdown);
      };
      return {
        toHtml: toHtml
      };
    } ());
    
    function updateHref(element, navItems){
      angular.forEach(element.find("a"), function (block) {
        var url = block.attributes['href'] && block.attributes['href'].value;
        if (!url) return;
        if (!urlService.isAbsoluteUrl(url))
          block.attributes['href'].value = urlService.getPageHref($location.path(), url, navItems);
      });
    }
    
    function transform(element, markdown, navItems) {
      var html = md.toHtml(markdown);
      element.html(html);
      angular.forEach(element.find("code"), function (block) {
        // use highlight.js to highlight code
        hljs.highlightBlock(block);
        // Add try button
        block = block.parentNode;
        if (angular.element(block).is("pre")) { //make sure the parent is pre and not inline code
          var wrapper = document.createElement("div");
          wrapper.className = "codewrapper";
          wrapper.innerHTML = '<div class="trydiv"><span class="tryspan">Try this code</span></div>';
          wrapper.childNodes[0].childNodes[0].onclick = function () {
            csplayService.tryCode(true, block.innerText);
          };
          block.parentNode.replaceChild(wrapper, block);
          wrapper.appendChild(block);
        }
      });
      updateHref(element, navItems);
      angular.forEach(element.find("img"), function (block) {
        var url = block.attributes['src'] && block.attributes['src'].value;
        if (!url) return;
        if (!urlService.isAbsoluteUrl(url))
          block.attributes['src'].value = urlService.getAbsolutePath($location.path(), url);
      });
      angular.forEach(element.find("table"), function (block) {
        angular.element(block).addClass('table table-bordered table-striped table-condensed');
      });
      angular.forEach(element.find("code-snippet", function (block) {
        var src = block.attributes['src'];
        var startLine = block.attributes['sl'];
        var endLine = block.attributes['el'];
        contentService.getMdContent(src).then(function(result){
          var data = result.data;
          var snippet = docUtility.substringLine(data, startLine, endLine);
          var wrapper = document.createElement("div");
          wrapper.innerHTML = snippet;
          block.parentNode.replaceChild(wrapper, block);
        });
      }));
      
      // Only return html() makes registered event, e.g. onclick missing
      // Instead, pass in element
    }
  }

  angular.module('docascode.markdownService', ['docascode.urlService', 'docascode.csplayService', 'docascode.contentService', 'docascode.util'])
    .service('markdownService', ['$q', '$http', '$location', 'urlService', 'csplayService', 'contentService', 'docUtility', provider]);

})();
/*
 * Define content load related functions used in docsController
 * Wrap Angular components in an Immediately Invoked Function Expression (IIFE)
 * to avoid variable collisions
 */

(function () {
  'use strict';
  /*jshint validthis:true */
  function provider($q, $http, $location, urlService, csplayService, contentService, utility) {
  
    this.loadMapInfo = loadMapInfo;
    
    function loadMapInfo(mapFilePath, model) {
      //        console.log("Start loading map file" + mapFilePath);
      return contentService.getMdContent(mapFilePath)
        .then(
        function (result) {
          if (!result) return;
          var data = result.data;
            
          // TODO: change md.map's key to "default" to make it much easier
          for (var key in data) {
            if (data.hasOwnProperty(key)) {
              var value = data[key];
              // 1. If it is the .map info for current model
              if (key === model.uid) {
                if (!model.map) model.map = value;
                else{
                  angular.extend(model.map, value);
                }
                loadMapInfoForEachItem(model.map);
              } else {
                // 2. If it is the .map info for model.children
                var itemModel = model.items[key];
                if (itemModel) {
                  if (!itemModel.map) itemModel.map = value;
                  else{
                    angular.extend(itemModel.map, value);
                  }
                  loadMapInfoForEachItem(itemModel.map);
                }
              }
            }
          }
          
          return model;
        },
        function (result) {

        }
        );
    }
    
    function loadMapInfoForEachItem(mapModel){
      if (mapModel.type === "yaml"){
        loadYamlMapInfoForEachItem(mapModel);
      }
      else if (mapModel.type === "markdown"){
        loadMarkdownMapInfoForEachItem(mapModel);
      }
    }
    
    function loadMarkdownMapInfoForEachItem(mapModel){
      if (!mapModel.content || !mapModel.references) return;
      var references = mapModel.references;
      var absolutePath = $location.path();
        var copied = mapModel.content;
        // replace the ones in references
        copied = processReferences(references, copied, mapModel, absolutePath);
        
        mapModel.content = copied;
    }

    function processReferences(references, markdownContent, mapModel, absolutePath) {
      if (!references) return;
      for (var key in references) {
        if (references.hasOwnProperty(key)) {
          var reference = references[key];
          var replacement = '';
          if (reference.type === 'codeSnippet') {
            // If path exists, it is CodeSnippet, need async load content
            if (reference.href) {
              var codeSnippetPath = urlService.getAbsolutePath(urlService.getContentFilePath(urlService.getPathInfo($location.path())), reference.href);
              var sl = reference.startLine;
              var el = reference.endLine;

              // Use mapModel ref for callback value setter
              contentService.getMdContent(codeSnippetPath).then(makeReplaceCodeSnippetFunction(mapModel, reference.Keys, sl, el));
            } else {
              var resolveErrorTag = 'Warning: Unable to resolve ' + reference.id + ': ' + reference.message;
              markdownContent = replaceAllKeys(reference.Keys, markdownContent, resolveErrorTag);
            }
          } else {
            var id = reference.id;
            // TODO: currently .map file is not generating the correct relative path
            var href = reference.href;
            replacement = "<a href='" + href + "'>" + id + "</a>";
            markdownContent = replaceAllKeys(reference.Keys, markdownContent, replacement);
          }
        }
      }
      return markdownContent;
    }

    function loadYamlMapInfoForEachItem(mapModel) {
      var path = mapModel.href;
      var startLine = mapModel.startLine;
      var endLine = mapModel.endLine;
      var references = mapModel.references;
      var absolutePath = urlService.getAbsolutePath($location.path(), path);
      contentService.getMdContent(absolutePath).then(function (result) {
        if (!result) return;
        var data = result.data;
        var section = utility.substringLine(data, startLine, endLine);
        var copied = section;
        // replace the ones in references
        if (references) {
          copied = processReferences(references, copied, mapModel, absolutePath);
        }
        
        mapModel.content = copied;
      });
    }
    
    function makeReplaceCodeSnippetFunction(mapModel, keys, startLine, endLine) {
      return function (result) {
        if (!result) return;
        var codeSnippet = utility.substringLine(result.data, startLine, endLine);
        // TODO: check if succeed
        var preCodeSnippetResolved = mapModel.content;
        mapModel.content = replaceAllKeys(keys, preCodeSnippetResolved, codeSnippet);
      };
    }

    function replaceAllKeys(keys, content, replacement) {
      for (var i = 0; i < keys.length; i++) {
        var reg = new RegExp(utility.escapeRegExp(keys[i]), 'g');
        content = content.replace(reg, replacement);
      }
      return content;
    }
  }

  angular.module('docascode.mapfileService', ['docascode.urlService', 'docascode.csplayService', 'docascode.contentService', 'docascode.util'])
    .service('mapfileService', ['$q', '$http', '$location', 'urlService', 'csplayService', 'contentService', 'docUtility', provider]);

})();
angular.module('docsApp', [
  'ngRoute',
  'ngSanitize',
  'itemTypes',

  'bootstrap',
  'ui.bootstrap',
  'ui.bootstrap.dropdown',
  
  'docascode.controller',
  'docascode.directives',
]);
var directive = {};

directive.runnableExample = ['$templateCache', '$document', function($templateCache, $document) {
  'use strict';
  var exampleClassNameSelector = '.runnable-example-file';
  var doc = $document[0];
  var tpl =
    '<nav class="runnable-example-tabs" ng-if="tabs">' +
    '  <a ng-class="{active:$index==activeTabIndex}"' +
    'ng-repeat="tab in tabs track by $index" ' +
    'href="" ' +
    'class="btn"' +
    'ng-click="setTab($index)">' +
    '    {{ tab }}' +
    '  </a>' +
    '</nav>';

  return {
    restrict: 'C',
    scope: true,
    controller: ['$scope', function($scope) {
      $scope.setTab = function(index) {
        var tab = $scope.tabs[index];
        $scope.activeTabIndex = index;
        $scope.$broadcast('tabChange', index, tab);
      };
    }],
    compile: function(element) {
      element.html(tpl + element.html());
      return function(scope, element) {
        var node = element[0];
        var examples = node.querySelectorAll(exampleClassNameSelector);
        var tabs = [],
          now = Date.now();
        angular.forEach(examples, function(child, index) {
          tabs.push(child.getAttribute('name'));
        });

        if (tabs.length > 0) {
          scope.tabs = tabs;
          scope.$on('tabChange', function(e, index, title) {
            angular.forEach(examples, function(child) {
              child.style.display = 'none';
            });
            var selected = examples[index];
            selected.style.display = 'block';
          });
          scope.setTab(0);
        }
      };
    }
  };
}];

directive.dropdownToggle =
  ['$document', '$location', '$window',
    function($document, $location, $window) {
      'use strict';
      var openElement = null,
        close;
      return {
        restrict: 'C',
        link: function(scope, element, attrs) {
          scope.$watch(function dropdownTogglePathWatch() {
            return $location.path();
          }, function dropdownTogglePathWatchAction() {
            if (close) close();
          });

          element.parent().on('click', function(event) {
            if (close) close();
          });

          element.on('click', function(event) {
            event.preventDefault();
            event.stopPropagation();

            var iWasOpen = false;

            if (openElement) {
              iWasOpen = openElement === element;
              close();
            }

            if (!iWasOpen) {
              element.parent().addClass('open');
              openElement = element;

              close = function(event) {
                if (event) event.preventDefault();
                if (event) event.stopPropagation();
                $document.off('click', close);
                element.parent().removeClass('open');
                close = null;
                openElement = null;
              };

              $document.on('click', close);
            }
          });
        }
      };
    }
  ];

directive.syntax = function() {
  'use strict';
  return {
    restrict: 'A',
    link: function(scope, element, attrs) {
      function makeLink(type, text, link, icon) {
        return '<a href="' + link + '" class="btn syntax-' + type + '" target="_blank" rel="nofollow">' +
          '<span class="' + icon + '"></span> ' + text +
          '</a>';
      }

      var html = '';
      var types = {
        'github': {
          text: 'View on Github',
          key: 'syntaxGithub',
          icon: 'icon-github'
        },
        'plunkr': {
          text: 'View on Plunkr',
          key: 'syntaxPlunkr',
          icon: 'icon-arrow-down'
        },
        'jsfiddle': {
          text: 'View on JSFiddle',
          key: 'syntaxFiddle',
          icon: 'icon-cloud'
        }
      };
      for (var type in types) {
        var data = types[type];
        var link = attrs[data.key];
        if (link) {
          html += makeLink(type, data.text, link, data.icon);
        }
      }

      var nav = document.createElement('nav');
      nav.className = 'syntax-links';
      nav.innerHTML = html;

      var node = element[0];
      var par = node.parentNode;
      par.insertBefore(nav, node);
    }
  };
};

directive.tabbable = function() {
  'use strict';
  return {
    restrict: 'C',
    compile: function(element) {
      var navTabs = angular.element('<ul class="nav nav-tabs"></ul>'),
        tabContent = angular.element('<div class="tab-content"></div>');

      tabContent.append(element.contents());
      element.append(navTabs).append(tabContent);
    },
    controller: ['$scope', '$element', function($scope, $element) {
      var navTabs = $element.contents().eq(0),
        ngModel = $element.controller('ngModel') || {},
        tabs = [],
        selectedTab;

      ngModel.$render = function() {
        var $viewValue = this.$viewValue;

        if (selectedTab ? (selectedTab.value !== $viewValue) : $viewValue) {
          if (selectedTab) {
            selectedTab.paneElement.removeClass('active');
            selectedTab.tabElement.removeClass('active');
            selectedTab = null;
          }
          if ($viewValue) {
            for (var i = 0, ii = tabs.length; i < ii; i++) {
              if ($viewValue === tabs[i].value) {
                selectedTab = tabs[i];
                break;
              }
            }
            if (selectedTab) {
              selectedTab.paneElement.addClass('active');
              selectedTab.tabElement.addClass('active');
            }
          }

        }
      };

      this.addPane = function(element, attr) {
        var li = angular.element('<li><a href></a></li>'),
          a = li.find('a'),
          tab = {
            paneElement: element,
            paneAttrs: attr,
            tabElement: li
          };

        tabs.push(tab);

        function update() {
          tab.title = attr.title;
          tab.value = attr.value || attr.title;
          if (!ngModel.$setViewValue && (!ngModel.$viewValue || tab === selectedTab)) {
            // we are not part of angular
            ngModel.$viewValue = tab.value;
          }
          ngModel.$render();
        }

        attr.$observe('value', update)();
        attr.$observe('title', function() {
          update();
          a.text(tab.title);
        })();

        navTabs.append(li);
        li.on('click', function(event) {
          event.preventDefault();
          event.stopPropagation();
          if (ngModel.$setViewValue) {
            $scope.$apply(function() {
              ngModel.$setViewValue(tab.value);
              ngModel.$render();
            });
          } else {
            // we are not part of angular
            ngModel.$viewValue = tab.value;
            ngModel.$render();
          }
        });

        return function() {
          tab.tabElement.remove();
          for (var i = 0, ii = tabs.length; i < ii; i++) {
            if (tab === tabs[i]) {
              tabs.splice(i, 1);
            }
          }
        };
      };
    }]
  };
};

directive.table = function() {
  'use strict';
  return {
    restrict: 'E',
    link: function(scope, element, attrs) {
      if (!attrs['class']) {
        element.addClass('table table-bordered table-striped table-condensed');
      }
    }
  };
};

var popoverElement = function() {
  'use strict';
  var object = {
    init: function() {
      this.element = angular.element(
        '<div class="popover popover-incode top">' +
        '<div class="arrow"></div>' +
        '<div class="popover-inner">' +
        '<div class="popover-title"><code></code></div>' +
        '<div class="popover-content"></div>' +
        '</div>' +
        '</div>'
      );
      this.node = this.element[0];
      this.element.css({
        'display': 'block',
        'position': 'absolute'
      });
      angular.element(document.body).append(this.element);

      var inner = this.element.children()[1];
      this.titleElement = angular.element(inner.childNodes[0].firstChild);
      this.contentElement = angular.element(inner.childNodes[1]);

      //stop the click on the tooltip
      this.element.on('click', function(event) {
        event.preventDefault();
        event.stopPropagation();
      });

      var self = this;
      angular.element(document.body).on('click', function(event) {
        if (self.visible()) self.hide();
      });
    },

    show: function(x, y) {
      this.element.addClass('visible');
      this.position(x || 0, y || 0);
    },

    hide: function() {
      this.element.removeClass('visible');
      this.position(-9999, -9999);
    },

    visible: function() {
      return this.position().y >= 0;
    },

    isSituatedAt: function(element) {
      return this.besideElement ? element[0] === this.besideElement[0] : false;
    },

    title: function(value) {
      return this.titleElement.html(value);
    },

    content: function(value) {
      if (value && value.length > 0) {
        value = marked(value);
      }
      return this.contentElement.html(value);
    },

    positionArrow: function(position) {
      this.node.className = 'popover ' + position;
    },

    positionAway: function() {
      this.besideElement = null;
      this.hide();
    },

    positionBeside: function(element) {
      this.besideElement = element;

      var elm = element[0];
      var x = elm.offsetLeft;
      var y = elm.offsetTop;
      x -= 30;
      y -= this.node.offsetHeight + 10;
      this.show(x, y);
    },

    position: function(x, y) {
      if (x != null && y != null) {
        this.element.css('left', x + 'px');
        this.element.css('top', y + 'px');
      } else {
        return {
          x: this.node.offsetLeft,
          y: this.node.offsetTop
        };
      }
    }
  };

  object.init();
  object.hide();

  return object;
};

directive.popover = ['popoverElement', function(popover) {
  'use strict';
  return {
    restrict: 'A',
    priority: 500,
    link: function(scope, element, attrs) {
      element.on('click', function(event) {
        event.preventDefault();
        event.stopPropagation();
        if (popover.isSituatedAt(element) && popover.visible()) {
          popover.title('');
          popover.content('');
          popover.positionAway();
        } else {
          popover.title(attrs.title);
          popover.content(attrs.content);
          popover.positionBeside(element);
        }
      });
    }
  };
}];

directive.tabPane = function() {
  'use strict';
  return {
    require: '^tabbable',
    restrict: 'C',
    link: function(scope, element, attrs, tabsCtrl) {
      element.on('$remove', tabsCtrl.addPane(element, attrs));
    }
  };
};

directive.foldout = ['$http', '$animate', '$window', function($http, $animate, $window) {
  'use strict';
  return {
    restrict: 'A',
    priority: 500,
    link: function(scope, element, attrs) {
      var container, loading, url = attrs.url;
      if (/\/build\//.test($window.location.href)) {
        url = '/build/docs' + url;
      }
      element.on('click', function() {
        scope.$apply(function() {
          if (!container) {
            if (loading) return;

            loading = true;
            var par = element.parent();
            container = angular.element('<div class="foldout">loading...</div>');
            $animate.enter(container, null, par);

            $http.get(url, {
              cache: true
            }).success(function(html) {
              loading = false;

              html = '<div class="foldout-inner">' +
                '<div calss="foldout-arrow"></div>' +
                html +
                '</div>';
              container.html(html);

              //avoid showing the element if the user has already closed it
              if (container.css('display') === 'block') {
                container.css('display', 'none');
                $animate.addClass(container, 'ng-hide');
              }
            });
          } else {
            if (container.hasClass('ng-hide')) $animate.removeClass(container, 'ng-hide');
            else $animate.addClass(container, 'ng-hide');
          }
        });
      });
    }
  };
}];

angular.module('bootstrap', [])
  .directive(directive)
  .factory('popoverElement', popoverElement)
  .run(function() {
    marked.setOptions({
      gfm: true,
      tables: true
    });
  });
(function() {
    'use strict';
     /*jshint validthis:true */
    function provider() {
        this.YamlExtension = '.yml';
        this.MdExtension = '.md';
        this.TocYamlRegexExp = /toc\.yml$/;
        this.YamlRegexExp = /\.yml$/;
        this.MdRegexExp = /\.md$/;
        this.MdOrYamlRegexExp = /(\.yml$)|(\.md$)/;
        this.MdIndexFile = '.map';
        this.TocFile = 'toc' + this.YamlExtension; // docConstants.TocFile
        this.TocAndFileUrlSeperator = '!'; // docConstants.TocAndFileUrlSeperator
    }

    angular.module('docascode.constants', [])
        .service('docConstants', provider);

})();
(function () {
  'use strict';

  angular.module('docascode.controller', ['docascode.contentService', 'docascode.urlService', 'docascode.directives', 'docascode.util', 'docascode.constants'])
    .controller('DocsController', [
    '$rootScope', '$scope', '$location', 'NG_ITEMTYPES', 'contentService', 'urlService', 'docUtility', 'docConstants',
    DocsController
  ]);

  function DocsController($rootScope, $scope, $location, NG_ITEMTYPES, contentService, urlService, docUtility, docConstants) {
    $rootScope.$on("activeNavItemChanged", function (event, args) {
      var selectedTocItem = args.active;
      var parent = args.parent;

    });
    $rootScope.$on("activeTocItemChanged", function (event, args) {
      var selectedTocItem = args.active;
      var parent = args.parent;
    });

    // watch for resize and reset height of side section
    /*$(window).resize(function () {
      $scope.$apply(function () {
        bodyOffset();
      });
    });*/
    
    /*function bodyOffset() {
      var navHeight = $('.topnav').height() + $('.subnav').height();
      $('.sidefilter').css('top', navHeight + 'px');
      $('.sidetoc').css('top', navHeight + 60 + 'px');
      $('.article').css('margin-top', navHeight + 30 + 'px');
    }*/
  }

})();
/*
 * Define the main functionality used in docsApp
 * Wrap Angular components in an Immediately Invoked Function Expression (IIFE)
 * to avoid variable collisions
 * Controller is *class*
 * Use controllers to
 *   * Set up the initial state of the $scope object
 *   * Add behavior to the $scope object
 * DONOT use controllers to
 *   * Manipulate DOM -- Controllers should contain only business logic
 *   * Format input -- Use *form* controls instead
 *   * Filter output -- Use *filter* instad
 *   * Share code or state across controllers -- Use *service* instead
 *   * Manage the life-cycle of other components
 */

(function() {
  'use strict';

  angular.module('docascode.controller')
    .controller('ContainerController', [
      '$rootScope', '$scope', '$location', 'NG_ITEMTYPES', 'contentService', 'urlService', 'docConstants',
      ContainerController
    ]);

  function ContainerController($rootScope, $scope, $location, NG_ITEMTYPES, contentService, urlService, docConstants) {

    // TODO: merge TOC&content loading to an animation/directive

    $scope.filteredItems = filteredItems;
    $scope.tocClass = tocClass;
    $scope.getTocHref = getTocHref;
    $scope.getNumber = function(num) { return new Array(num + 1); };

    $scope.toc = null;
    $scope.content = null;
    $rootScope.$on("navbarLoaded", function(event, args){
      if (args && args.navbar && angular.isArray(args.navbar)){
        args.navbar.forEach(function(element) {
          element.href = urlService.normalizeUrl(element.href);
        }, this);
        $scope.navbar = args.navbar;
      }
    });
    $scope.$watch(function(){return $location.path();}, function(path){
      var pathInfo = urlService.getPathInfo(path);
      var contentPath = urlService.getContentFilePath(pathInfo);
      $scope.currentContentPath = pathInfo ? pathInfo.contentPath : null;
      $scope.currentTocPath = pathInfo ? pathInfo.tocPath : null;
      loadContent(contentPath);
    });

    $rootScope.$on("activeNavItemChanged", function(event, args){
      $scope.toc = null;
      if ($scope.contentType === 'error') $scope.contentType = 'success';
      var currentNavItem = args.active;
      $scope.currentHomepage = currentNavItem.homepage;
      var currentNavbar = args.active;
      var tocPath = currentNavbar.href + '/' + docConstants.TocFile;

      contentService.getTocContent(tocPath).then(
        function(data){
          if (data.content){
            $scope.toc = data;
          }
        });
    });
    
    $rootScope.$on("activeNavItemError", function(event, args){
      $scope.contentType = 'error';
      $scope.toc = null;
    });
    
    $scope.$watchGroup(['tocPage', 'currentHomepage'], function(newValues, oldValues){
      if (!newValues) return;
      var tocPage = newValues[0];
      var currentHomepage = newValues[1];
      if (!tocPage) return;
      var scope = $scope;
      if (scope.contentType === 'error') return;
      
      // If current homepage exists, use homepage
      if (currentHomepage) {
        scope.contentPath = currentHomepage;
        scope.contentType = 'markdown';
      } else {
        scope.contentPath = tocPage;
        scope.contentType = 'toc';
      }
    });

    function loadContent(path){
        var scope = $scope;
        if (path) {
          // If is toc.yml and home page exists, set to $scope and return
          // TODO: refactor using ngRoute
          scope.tocPage = (docConstants.TocYamlRegexExp).test(path) ? path : '';
          if (scope.tocPage) return;
          scope.contentPath = path;
          
          // If end with .md
          if ((docConstants.MdRegexExp).test(path)) {
            // TODO: Improve TITLE
            scope.title = path;
            scope.contentType = 'markdown';
          } else if ((docConstants.YamlRegexExp).test(path)) {
            scope.contentType = 'yaml';
          } else{
            scope.contentType = 'error';
          }
          // If not md or yaml, simply try load the path
        }
    }
    // Href relative to current toc file
    function getTocHref(url) {
      var currentTocPath = $scope.currentTocPath;
      if (!currentTocPath) return null;
      return urlService.getHref(currentTocPath, '', url);
    }

    function filteredItems(f) {
      /* jshint validthis: true */
      var globalVisible = !f;
      this.toc.content.forEach(function(a, i, o) {
        // show namespace if any of its child is visible
        // show all the children if the namespace is visible
        var firstLevelTocName = a.uid || a.name;
        var hide = !globalVisible && !filterNavItem(firstLevelTocName, f);
        var tempHide = hide;
        if (a.items){
          a.items.forEach(function(a1, i1, o1){
            // support firstLevel.lastLevel format seach
            var lastLevelFullName = firstLevelTocName + '.' + a1.name;
            a1.hide = tempHide && !filterNavItem(lastLevelFullName, f);
            if (!a1.hide){
              // show firstLevel if any of its children is visible
              hide = false;
            }
          });
        }

        a.hide = hide;
      });
    }

    function filterNavItem(name, text) {
      if (!text) return true;
      if (name.toLowerCase().indexOf(text.toLowerCase()) > -1) return true;
      return false;
    }

    /****************************************
     element ng-class related Implementation
     ****************************************/
    function tocClass(selectedItem) {
      /* jshint validthis: true */
      var currentContentPath = $scope.currentContentPath;
      if (!currentContentPath) return null;
      
      var current = {
        active: selectedItem.href && currentContentPath === selectedItem.href,
        'nav-index-section': selectedItem.type === 'section'
      };

      if (current.active === true) {
        var currentTocSelectedItem = $scope.currentTocSelectedItem;
        if (selectedItem && currentTocSelectedItem !== selectedItem){
          $scope.currentTocSelectedItem = currentTocSelectedItem = selectedItem;
          var selectedItemInfo = {
            active: selectedItem,
          };

          /* Use this.navGroup and this.navItem to get current selected item and its parent if has */
          if (selectedItem === this.navItem){
            selectedItemInfo.parent = this.navGroup;
          }

          $rootScope.$broadcast("activeTocItemChanged", selectedItemInfo);
        }
      }
      return current;
    }
  }
})();
(function() {
  'use strict';

  angular.module('docascode.controller')
    .controller('NavbarController', [
      '$rootScope', '$scope', '$location', 'NG_ITEMTYPES', 'contentService', 'urlService', 'docConstants',
      NavbarController
    ]);

  function NavbarController($rootScope, $scope, $location, NG_ITEMTYPES, contentService, urlService, docConstants) {
    $scope.navClass = navClass;
    $scope.getNavHref = getNavHref;
    $scope.getBreadCrumbHref = getBreadCrumbHref;

    $rootScope.$on("activeTocItemChanged", function(event, args) {
      var selectedTocItem = args.active;
      var parent = args.parent;
      breadCrumbWatcher($scope.currentNavItem, parent, selectedTocItem);
    });

    contentService.getNavBar().then(
      function(data) {
        var navbar = data;
        
        $scope.model = navbar;
        $rootScope.$broadcast("navbarLoaded", {
          navbar: data
        });


        $scope.$watch(function(){return $location.path();}, function(path){
          
          if (!path && navbar && navbar.length > 0 && navbar[0].href) 
            $location.url(navbar[0].href);
        
          var pathInfo = urlService.getPathInfo(path);
          if (pathInfo) {
            var navPath = pathInfo.tocPath || pathInfo.contentPath;
            var navItem = $scope.model.filter(function(x){ return urlService.urlAreEqual(x.href, navPath);})[0];
            breadCrumbWatcher(navItem);
            if (navItem) {
              var currentNavItem = $scope.currentNavItem;
              if (navItem && currentNavItem !== navItem) {
                $scope.currentNavItem = currentNavItem = navItem;
                $rootScope.$broadcast("activeNavItemChanged", {
                  active: navItem
                });
              }
            } else {
              // path does not match a valid navbar item
              $scope.currentNavItem = null;
              $rootScope.$broadcast("activeNavItemError");
            }
          }
        });
      },
      function(data) {
        $rootScope.$broadcast("navbarError", {
          // TODO： what to do with navbar error?
        });
      });
    function breadCrumbWatcher(currentNavItem, currentGroup, currentPage) {
      // breadcrumb generation logic
      var breadcrumb = $scope.breadcrumb = [];

      if (currentNavItem) {
        breadcrumb.push({
          name: currentNavItem.name,
          // use '/#/' to indicate this is a nav link...
          url: '/#/' + currentNavItem.href
        });
      }

      if (currentGroup) {
        breadcrumb.push({
          name: currentGroup.uid || currentGroup.name,
          url: currentGroup.href
        });
      }

      // If toc does not exist, use navbar's title
      // No need to set url as the last one is the current one
      if (currentPage) {
        breadcrumb.push({
          name: currentPage.name,
          // url: currentPage.href
        });
      }
    }

    // Href relative to current toc file
    function getBreadCrumbHref(url) {
      // For navbar url, no need to calculate relative path from toc
      if (url && url.indexOf('/#/') === 0) return url.substring(1);
      var currentPath = $location.path();
      var pathInfo = urlService.getPathInfo(currentPath);
      return urlService.getHref(pathInfo.tocPath, '', url);
    }

    function navClass(navItem) {
      var navPath;
      var currentPath = $location.path();
      var pathInfo = urlService.getPathInfo(currentPath);
      if (pathInfo) {
        navPath = urlService.normalizeUrl(pathInfo.tocPath || pathInfo.contentPath);
      }

      var current = {
        active: navPath && navPath === navItem.href,
      };

      return current;
    }

    function getNavHref(url) {
      if (urlService.isAbsoluteUrl(url)) return url;
      if (!url) return '';
      return '#' + url;
    }

  }
})();
// Meta data used by the AngularJS docs app
angular.module('pagesData', [])
  .value('NG_PAGES', {});
// Order matters
angular.module('itemTypes', [])
  .value('NG_ITEMTYPES', {
    "class": {
      "Constructor": {
        "id": "ctor",
        "name": "Constructor",
        "description": "Constructors",
        "show": false
      },
      "Field": {
        "id": "field",
        "name": "Field",
        "description": "Fields",
        "show": false
      },
      "Property": {
        "id": "property",
        "name": "Property",
        "description": "Properties",
        "show": false
      },
      "Method": {
        "id": "method",
        "name": "Method",
        "description": "Methods",
        "show": false
      },
      "Operator": {
        "id": "operator",
        "name": "Operator",
        "description": "Operators",
        "show": false
      },
      "Event": {
        "id": "event",
        "name": "Event",
        "description": "Events",
        "show": false
      }
    },
    // [
    //   { "name": "Property", "description": "Property" },
    //   { "name": "Method" , "description": "Method"},
    //   { "name": "Constructor" , "description": "Constructor"},
    //   { "name": "Field" , "description": "Field"},
    // ],
    "namespace": {
      "Class": {
        "id": "class",
        "name": "Class",
        "description": "Classes",
        "show": false
      },
      "Enum": {
        "id": "enum",
        "name": "Enum",
        "description": "Enums",
        "show": false
      },
      "Delegate": {
        "id": "delegate",
        "name": "Delegate",
        "description": "Delegates",
        "show": false
      },
      "Interface": {
        "id": "interface",
        "name": "Interface",
        "description": "Interfaces",
        "show": false
      },
      "Struct": {
        "id": "struct",
        "name": "Struct",
        "description": "Structs",
        "show": false
      },
    },
    // [

    //   { "name": "Class", "description": "Class" },
    //   { "name": "Enum" , "description": "Enum"},
    //   { "name": "Delegate" , "description": "Delegate"},
    //   { "name": "Struct" , "description": "Struct"},
    //   { "name": "Interface", "description": "Interface" },
    // ]
  });
/*
 * Define directives used in docsApp
 * Wrap Angular components in an Immediately Invoked Function Expression (IIFE)
 * to avoid variable collisions
 */

(function () {
  'use strict';

  angular.module('docascode.directives', ['itemTypes', 'docascode.urlService', 'docascode.util', 'docascode.markdownService','docascode.mapfileService', 'docascode.csplayService', 'docascode.contentService'])
  /**
   * backToTop Directive
   * @param  {Function} $anchorScroll
   *
   * @description Ensure that the browser scrolls when the anchor is clicked
   */
    .directive('backToTop', ['$anchorScroll', '$location', function ($anchorScroll, $location) {
    return function link(scope, element) {
      element.on('click', function (event) {
        $location.hash('');
        scope.$apply($anchorScroll);
      });
    };
  }])
    .directive('scrollYOffsetElement', ['$anchorScroll', function ($anchorScroll) {
    return function (scope, element) {
      $anchorScroll.yOffset = element;
    };
  }])
    .directive('affixBar', function () {
    var template =
      '<nav class="affix">' +
      '<h3>{{title}}</h3>' +
      '<ul class="nav">' +
      '<li scroll-link="{{child.htmlId}}" ng-attr-last="{{$last}}" ng-attr-first="{{$first}}" ng-repeat="child in model">' +
      '<a>{{ child.title | limitTo: 22 }}{{child.title.length > 22 ? "..." : ""}}</a>' +
      '<ul class="nav">' +
      '<li scroll-link="{{item.htmlId}}" ng-attr-last="{{$last}}" ng-attr-first="{{$first}}" ng-repeat="item in child.items">' +
      '<a>{{ item.title | limitTo: 22 }}{{item.title.length > 22 ? "..." : ""}}</a>' +
      '</li>' +
      '</ul>' +
      '</li>' +
      '</ul>' +
      '</nav>';

    return {
      restrict: 'E',
      replace: true,
      template: template,
      priority: 10, 
      scope:{
        title:"@",
        model:"=data"
      },
    };
  })
    .directive('scrollLink', ['$timeout', '$window', '$location', '$document', '$anchorScroll', function ($timeout, $window, $location, $document, $anchorScroll) {
    // get offset from anchorScroll's yOffset
    function getYOffset() {
      var offset = $anchorScroll.yOffset;
      if (angular.isFunction(offset)) {
        offset = offset();
      } else if (angular.isElement(offset)) {
        var elem = offset[0];
        var style = $window.getComputedStyle(elem);
        if (style.position !== 'fixed') {
          offset = 0;
        } else {
          offset = elem.getBoundingClientRect().bottom;
        }
      } else if (!angular.isNumber(offset)) {
        offset = 0;
      }

      return offset;
    }

    function getActiveScroll(id, isFirst, isLast) {
      var element = angular.element("#" + id);
      if (element && element.length > 0) {
        if (isFirst && $window.scrollY === 0){
          return true;
        }
        
        if (isLast && ($window.scrollY + $window.innerHeight === $document.height())) {
          return true;
        }

        var top = element.offset().top;
        var bottom = top + element.height();
        var yOffset = getYOffset();
        var pageYOffset = $window.pageYOffset + yOffset;
        // The below one will override the upper one
        if (top <= pageYOffset)
          return true;
      }

      return false;
    }

    function setActive(liElement) {
      if (!liElement) return;
      var ulElement = liElement.parent();
      // for all the descendant and sibling <li>'s remove active
      angular.forEach(ulElement.children("li"), function (block) {
        if (block === liElement[0]) {
          angular.element(block).addClass("active");
        }
        else {
          var ele = angular.element(block);
          ele.removeClass("active");
          angular.forEach(ele.find("li"), function (block) {
            angular.element(block).removeClass("active");
          });
        }
      });
    }

    function scrollTo(element, id) {
      if ($location.hash() !== id) {
        // trick provided by http://stackoverflow.com/questions/11784656/angularjs-location-not-changing-the-path
        $timeout(function () {
          $location.hash(id);
        }, 1);
      } else {
        $anchorScroll();
      }
      setActive(element);
    }

    return {
      restrict: 'A',
      link: function (scope, element, attrs, ngModel) {
        var id = attrs.scrollLink;
        var last = attrs.last === "true";
        var first = attrs.first === "true";
        var active = getActiveScroll(id, first, last);
        if (active) {
          setActive(element);
        }
          
        // get <a> children if exists
        // children() travels a single level down the DOM tree
        // while find() search through the descendants of the DOM
        var linkElement = element.children('a')[0];
        if (linkElement) {
          linkElement.onclick = function () {
            scrollTo(element, id);
          };
        }

        angular.element($window).bind('scroll', function () {
          var active = getActiveScroll(id, first, last);
          if (active) {
            setActive(element);
          }
        });
      }
    };
  }])
    .directive('code', function () {
    return {
      restrict: 'E',
      require: 'ngModel',
      scope: {
        bindonce: "@",
      },
      terminal: true,
      link: function (scope, element, attrs, ngModel) {
        var unwatch = scope.$watch(function () { return ngModel.$modelValue; }, function (value, oldValue) {
          if (value === undefined) return;
          var language;
          var content;
          if (value.CSharp) {
            language = "csharp";
            content = value.CSharp;
          } else if (value.VB) {
            language = "vb";
            content = value.VB;
          }

          element.text(content);
          angular.forEach(element, function (block) {
            hljs.highlightBlock(block, language);
          });
          if (scope.bindonce) {
            unwatch();
          }
        });
      }
    };
  })
  .directive('compositeLink', ['$location', 'urlService', 'docUtility', function ($location, urlService, util) {
    var template =
      '<a ng-repeat="item in nameList" ng-href="{{item.href}}" ng-class="{disable:!item.href}">{{item.name}}</a>';
      
    // Href relative to current file
    function getLinkHref(url) {
      return urlService.getLinkHref(url, $location.path());
    }
    
    function getNameList(item, language){
      if (!item) return null;
      var nameList = [];
      var name = util.getNameWithSelector('name', language, item) || item.uid;
      var spec = util.getNameWithSelector('spec', language, item);
      if (!name && !spec) return null;
      // A complex name e.g. Tuple<string,int> that is formed by a list of simple type 
      if (spec){
        if(angular.isArray(spec)){
          spec.forEach(function(element) {
            var nameItem = {href: getLinkHref(element.href), name: util.getDisplayName(element, language)};
            nameList.push(nameItem);
          });
        }else{
          console.error("spec is expected to be array, however it is " + spec);
        }
      }else{
          var nameItem = {href: getLinkHref(item.href), name: util.getDisplayName(item, language)};
          nameList.push(nameItem);
      }
      return nameList;
    }
   
    return {
      restrict: 'E',
      replace: true,
      template: template,
      priority: 10, 
      scope:{
        model: "=ngData",
        lang: "=ngLanguage"
      },
      link: function (scope, element, attrs) {
        scope.$watch("model", function (value, oldValue) {
          if (value === undefined) return;
          scope.nameList = getNameList(value, scope.lang);
        });
        scope.$watch("lang", function (value, oldValue) {
          if (value === undefined) return;
          scope.nameList = getNameList(scope.model, value);
        });
      }
    };
  }]);
})();
(function () {
  'use strict';

  angular.module('docascode.directives')
  /**
   * yamlContent Directive
   * @param  {Function} contentService
   * @param  {Function} urlService
   *
   * @description Render a page with .md file, supporting try...code
   */
    .directive('yamlContent', ['NG_ITEMTYPES', '$location', 'contentService', 'urlService', 'docUtility', 'mapfileService', yamlContent]);

  function yamlContent(NG_ITEMTYPES, $location, contentService, urlService, utility, mapfileService) {

    function getImproveTheDocHref(mapItem) {
      /* jshint validthis: true */
      if (!mapItem) return '';
      return urlService.getRemoteUrl(mapItem.remote, mapItem.startLine + 1);
    }

    function getViewSourceHref(model) {
      /* jshint validthis: true */
      if (!model || !model.source || !model.source.remote) return '';
      return urlService.getRemoteUrl(model.source.remote, model.source.startLine + 1);
    }

    function getNumber(num) {
      return new Array(num + 1);
    }
  
    // Href relative to current file
    function getLinkHref(url) {
      return urlService.getLinkHref(url, $location.path());
    }
    
    function getDisplayName(element, language) {
      return utility.getDisplayName(element, language);
    }
    
    // expand / collapse all logic for model items
    function expandAll(model, state) {
      if (model && model.items) {
        var items = model.items;
        angular.forEach(items, function(item){
          angular.forEach(item.items, function(i){
            i.showDetail = state;
          });
        });
      }
    }
    
    /**************************************/
    function getMatchedItem(key, references) {
      var matched = references.filter(function (x) {return x.uid === key;})[0] || {};
      if (matched.uid) {
        return matched;
      } 
      return null;
    }
    
    // Parameters/exceptions as an array for object {id, type, description}
    function setArrayTypeModel(parameters, references){
      if (!parameters || parameters.length === 0) return null;
      parameters.forEach(function(element) {
        // type is uid for 
        var matched = getMatchedItem(element.type, references);
        if (matched && matched.uid){
          element.typeModel = matched; 
        } else {
          // If no matching item is found, using element.type as name
          element.typeModel = {name: element.type};
        }
      });
    }
    
    function setReturnTypeModel(returnValue, references){
      if (!returnValue) return null;
      var matched = getMatchedItem(returnValue.type, references);
      if (matched && matched.uid){
        returnValue.typeModel = matched;
      } else {
        // If no matching item is found, using element.type as name
        returnValue.typeModel = {name: returnValue.type};
      }
    }
    
    function render(scope, element, yamlFilePath, loadMapFile) {
      if (!yamlFilePath) return;
      scope.contentType = '';
      scope.model = {};
      scope.title = '';
      contentService.getContent(yamlFilePath).then(function (data) {
        var items = data.items;
        var references = utility.getArray(data.references, function(r, item) {item.uid = r;}) || [];

        // TODO: what if items are not in order? what if items are not arranged as expected, e.g. multiple namespaces in one yml?
        var item = items[0];
        var allItems;
        // May be itself
        references = items.concat(references || []);
        
        // Get children
        if (item.children) {
          var children = {};
          for (var i = 0, l = item.children.length; i < l; i++) {
            var matched = getMatchedItem(item.children[i], references);
            if (matched && matched.uid) {
              children[matched.uid] = matched;
            }
          }
          allItems = children;
        }
        
        // Get inheritance=>array of UID
        var inheritance = item.inheritance;
        if (inheritance) {
          var inheritanceModel = [];
          inheritance.forEach(function(element) {
            var matched = getMatchedItem(element, references);
            if (matched && matched.uid) {
              inheritanceModel.push(matched);
            }
          }, this);
            
          item.inheritanceModel = inheritanceModel;
        }
        
        var syntax = item.syntax;
        if (syntax){
          // Set type model for parameter's type
          setArrayTypeModel(syntax.parameters, references);
          
          // Set type model for return's type
          setReturnTypeModel(syntax.return, references);
        }
        
        // Set type model for exception's type
        setArrayTypeModel(item.exceptions, references);
        
        
        // Set type model for children's parameter's & return's type & exception type
        for (var itemKey in allItems){
          if (allItems.hasOwnProperty(itemKey) && allItems[itemKey]) {
            setArrayTypeModel(allItems[itemKey].exceptions, references);
            if (allItems[itemKey].syntax) {
              setArrayTypeModel(allItems[itemKey].syntax.parameters, references);
              setReturnTypeModel(allItems[itemKey].syntax.return, references);
            }
          }
        }
        
        // For View model
        if (item.type.toLowerCase() === 'namespace') {
          scope.contentType = 'namespace';
        } else {
          scope.contentType = 'class';
        }
        
        var childrenModel = [];
        if (allItems){
          var displayTypes = scope.contentType === 'namespace' ? NG_ITEMTYPES.namespace : NG_ITEMTYPES.class;
          for (var key in displayTypes){
            if (displayTypes.hasOwnProperty(key)) {
              var displayType = displayTypes[key];
              var htmlId = escapeId(displayType.id);

              // htmlId and title are for affix display
              var subModel = {value: displayType, title: displayType.description, htmlId: htmlId, items: []};
              for (var keys in allItems){
                if (allItems.hasOwnProperty(keys)){
                  var currentItem = allItems[keys];
                  if (currentItem.type === displayType.name){
                    // NOTE: escape ID for html
                    // htmlId and title are for affix display
                    currentItem.htmlId = escapeId(currentItem.uid);
                    currentItem.title = getDisplayName(currentItem, scope.lang);
                    subModel.items.push(currentItem);
                  }
                }
              }
              if (subModel.items.length > 0){
                childrenModel.push(subModel);
              }
            }
          }
        }
        
        if (childrenModel.length > 0) item.items = childrenModel;
        scope.model = item;
        // For affix:
        var firstItem = {title: "Summary", htmlId: "article-header"};
        scope.affixModel = [firstItem].concat(item.items);
        scope.title = getDisplayName(item, scope.lang);
        if (loadMapFile) {
          mapfileService.loadMapInfo(yamlFilePath + ".map", scope.model);
        }
      }).catch(function(){
        scope.contentType = 'error';
      }
      );
    }
    
    // Href relative to current toc file
    function getTocHref(url) {
      // if (!$scope.model) return '';
      var currentPath = $location.path();
      var pathInfo = urlService.getPathInfo(currentPath);
      pathInfo.contentPath = '';
      return urlService.getHref(pathInfo.tocPath, '', url);
    }
    
    function escapeId(id) {
      // html id attr only allows a-z A-Z 0-9 . : _
      // while . : are valid selectors
      if (!id) return id;
      return id.replace(/[^a-zA-Z0-9_]/g, '_');
    }
    
    function YamlContentController($scope) {
      $scope.getViewSourceHref = getViewSourceHref;
      $scope.getImproveTheDocHref = getImproveTheDocHref;
      $scope.getLinkHref = getLinkHref;
      $scope.expandAll = expandAll;
      $scope.getNumber = getNumber;
      $scope.getTocHref = getTocHref;
      $scope.getDisplayName = getDisplayName;
    }

    YamlContentController.$inject = ['$scope'];
    return {
      restrict: 'E',
      replace: true,
      templateUrl: 'template/yamlContent.html',
      priority: 100,
      require: 'ngModel',
      scope: {
        getMap: "=",
        lang: "=ngLanguage",
        navbar: "=ngNavbar"
      },
      controller: YamlContentController,
      link: function (scope, element, attrs, ngModel) {
        var localScope = scope;
        scope.$watch(function () { return ngModel.$modelValue; }, function (value, oldValue) {
          if (value === undefined) return;
          render(localScope, element, value, localScope.getMap);
        });
      }
    };
  }
})();
(function () {
  'use strict';

  angular.module('docascode.directives')
  /**
   * markdownContent Directive
   * @param  {Function} contentService
   * @param  {Function} markdownService
   * @param  {Function} urlService
   *
   * @description Render a page with .md file, supporting try...code
   */
    .directive('markdown', ['contentService', 'markdownService', 'urlService', markdown])
    .directive('markdownContent', ['contentService', 'markdownService', 'urlService', 'mapfileService', markdownContent]);
  function markdown(contentService, markdownService, urlService, mapfileService){
    function render(element, content, navbar) {
      markdownService.transform(element, content, navbar);
    }
    
    function loadSrc(element, value, navbar) {
      if (!value) return;
      element.html('');

      contentService.getMarkdownContent(value)
        .then(
        function (result) {
          render(element, result, navbar);
        },
        function (result) {
          element.html(result.data);
        }
        );
    }
    
    return {
      restrict: 'AE',
      link: function (scope, element, attrs) {
        if (attrs.src) {
          // loadSrc(scope, element, attrs.src);
          scope.$watch(attrs.src, function (value, oldValue) {
            loadSrc(element, value, scope.navbar);
          });
        }
        
        if (attrs.data) {
          // render(element, attrs.data);
          scope.$watch(attrs.data, function (value, oldValue) {
            if (value === undefined) return;
            render(element, value, scope.navbar);
          });
        }
        
        scope.$watch("navbar", function(value, oldValue){
          if(value === undefined) return;
          markdownService.updateHref(element, value); 
        });
      }
    };
  }
  
  function markdownContent(contentService, markdownService, urlService, mapfileService) {
    var template =
      '<div>' +
      '<div ng-class="{\'col-sm-9\':affixModel, \'col-md-10\':affixModel}">' +
      '<a ng-if="href" ng-href="{{href}}" class="btn btn-primary pull-right mobile-hide">' +
      '<!--<span class="glyphicon glyphicon-edit">&nbsp;</span>-->Improve this Doc' +
      '</a>' +
      '<article></article>' +
      '</div>' +
      '<div class="hidden-xs col-sm-3 col-md-2" ng-if="affixModel">' +
      '<affix-bar title="In this article" id="docs-subnavbar" data="affixModel">' +
      '</affix-bar>' +
      '</div>' +
      '</div>'
      ;
      
    function transform(scope, wrapElement, mapModel, navbar){
      var element = angular.element(wrapElement.find('article')[0]);
      var content = mapModel.content;
          markdownService.transform(element, content, navbar);
          // Build Affix Bar:
          // Ignore h1, h2 as top level, h3 as leaf level
          var affixModel = [];
          var currentH2Item;
          angular.forEach(element.find('h2,h3'), function(header){
            var ele = angular.element(header);
            if (ele.is('h2')){
              // If is top level
              currentH2Item = {htmlId: header.id, title:header.innerText, items:[]};
              affixModel.push(currentH2Item);
            } else {
              // If is leaf level
              if (!currentH2Item){
                // If does not have h2 in previous section, use a default one
                currentH2Item = {title:"Summary", items:[]};
                affixModel.push(currentH2Item);
              }
              var h3Item = {htmlId: header.id, title: header.innerText};
              currentH2Item.items.push(h3Item);
            }
          });
          angular.forEach(affixModel, function(item){
            if (item.items.length === 0) item.items = null;
          });          
          if (affixModel.length === 0) affixModel = null;
          scope.affixModel = affixModel;
    }
    
    function loadSrc(scope, element, value, loadMapFile) {
      if (!value) return;
      contentService.getMarkdownContent(value)
        .then(
        function (result) {
          scope.map = {content: result};
          scope.uid = "default";
          var mapFilePath = value + ".map";
          // console.log("Start loading map file" + mapFilePath);
          if (loadMapFile) {
            mapfileService.loadMapInfo(mapFilePath, scope).then(
              function(result){
                var map = result.map;
                if (map){
                  scope.href = urlService.getRemoteUrl(map.remote, map.startLine + 1); 
                }
              }
            );
          }
        },
        function (result) {
          transform(scope, element, result.data, scope.navbar);
        }
        );
    }
    
    function render(scope, element, markdownFilePath, loadMapFile, mapfileService) {
      if (!markdownFilePath) return;      
      var article = angular.element(element.find('article')[0]);
      article.html('');
      loadSrc(scope, element, markdownFilePath, loadMapFile);
    }
    return {
      restrict: 'E',
      replace: true,
      template: template,
      priority: 100,
      require: 'ngModel',
      scope:{
        getMap: "=",
        navbar: "=ngNavbar"
      },
      link: function (scope, element, attrs, ngModel) {
        var localScope = scope;
        // render(localScope, element, attrs.src, getMap);
        scope.$watch(function () { return ngModel.$modelValue; }, function (value, oldValue) {
          if (value === undefined) return;
          render(localScope, element, value, localScope.getMap, mapfileService);
        });
        scope.$watch("map.content", function(value, oldValue){
          if (value === undefined) return;
          transform(scope, element, scope.map, scope.navbar);
        });
        scope.$watch("navbar", function(value, oldValue){
          if (value === undefined) return;
          markdownService.updateHref(element, value);
        });
      }
    };
  }
})();
(function () {
  'use strict';
  /*jshint validthis:true */
  function utilityProvider() {
    this.cleanArray = function (actual) {
      var newArray = [];
      for (var i = 0; i < actual.length; i++) {
        if (actual[i]) {
          newArray.push(actual[i]);
        }
      }
      return newArray;
    };
    
    this.getArray = function (items, callback) {
      if (angular.isArray(items)) {
        return items;
      } else {
        var array = [];
        for (var key in items) {
          if (items.hasOwnProperty(key)) {
            if(callback) callback(key, items[key]);
            array.push(items[key]);
          }
        }
        return array;
      }
    };

    // StartLine starts @1 to be consistent with IDE
    this.substringLine = function (input, startline, endline) {
      if (!input || endline < 1 || startline > endline) return '';
      var lines = input.split('\n');
      var maxLine = lines.length;
      startline = startline <= 1 ? 1 : startline;
      endline = endline >= maxLine ? maxLine : endline;
      var snippet = '';
      for (var i = startline - 1; i < endline; i++) {
        snippet += lines[i] + '\n';
      }
      return snippet;
    };
    
    this.escapeRegExp = function (str) {
      return str.replace(/[\-\[\]\/\{\}\(\)\*\+\?\.\\\^\$\|]/g, "\\$&");
    };
    
    // Start from here: YAML specific
    this.getNameWithSelector = function(defaultNameSelector, language, model){
      if (!defaultNameSelector || !model) return null;
      var nameSelector = defaultNameSelector;
      
      // Use the language specific one, if it does not exist, fallback to the default one;
      if (language) nameSelector = defaultNameSelector + '.'+language;
      return model[nameSelector] || model[defaultNameSelector];
    };
    
    this.getDisplayName = function(item, language){
      if (!item) return null;
      var name = this.getNameWithSelector("name", language, item);
      
      // if item does not have href, use full name
      if (!item.href){
        return this.getNameWithSelector("fullName", language, item) || name || item.uid ;
      } else return name; 
    };
    
  }

  angular.module('docascode.util', [])
    .service('docUtility', utilityProvider);
})();
// Meta data used by the AngularJS docs app
angular.module('versionsData', [])
  .value('NG_VERSION', {
    "raw": "1.0.0",
    "major": 1,
    "minor": 0,
    "patch": 0,
    "prerelease": [
      "local"
    ],
    "build": "sha.8200011",
    "version": "1.0.0-master",
    "branch": "master"
  })
  .value('NG_VERSIONS', [{
    "raw": "1.0.0",
    "major": 1,
    "minor": 0,
    "patch": 0,
    "prerelease": [
      "local"
    ],
    "build": "sha.8200011",
    "version": "1.0.0-master",
    "branch": "master"
  }]);
angular.module('versions', [])

.controller('DocsVersionsCtrl', ['$scope', '$location', '$window', 'NG_VERSIONS', function($scope, $location, $window, NG_VERSIONS) {
  'use strict';
  $scope.docs_version = NG_VERSIONS[0];
  $scope.docs_versions = NG_VERSIONS;

  for (var i = 0, minor = NaN; i < NG_VERSIONS.length; i++) {
    var version = NG_VERSIONS[i];
    // NaN will give false here
    if (minor <= version.minor) {
      continue;
    }
    version.isLatest = true;
    minor = version.minor;
  }

  $scope.getGroupName = function(v) {
    return v.isLatest ? 'Latest' : ('v' + v.major + '.' + v.minor + '.x');
  };

  $scope.jumpToDocsVersion = function(version) {
    var currentPagePath = $location.path().replace(/\/$/, '');

    // TODO: We need to do some munging of the path for different versions of the API...


    $window.location = version.docsUrl + currentPagePath;
  };
}]);