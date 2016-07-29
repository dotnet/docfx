// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
/*
 * Define doc related functions used in docsApp
 * Wrap Angular components in an Immediately Invoked Function Expression (IIFE)
 * to avoid variable collisions
 */

(function() {
  'use strict';
   /*jshint validthis:true */
  function provider($q, $http, docConstants, docUtility) {
    function normalizeUrl(url) {
      if (!url) return '';
      var absoluteUrlRegExp = new RegExp('^(?:[a-z]+:)?//', 'i');
      var match = url.match(absoluteUrlRegExp);
      var prefix = "";
      if (match) {
        prefix = match[0];
        url = url.slice(prefix.length);
      }
      var arr = url.split(/[/|\\]/);
      var newArray = docUtility.cleanArray(arr);
      return prefix + newArray.join('/');
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

      // separate toc and content with !
      var index = currentPath.indexOf(docConstants.TocAndFileUrlSeparator);
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
      var path = pathInfo.tocPath ? pathInfo.tocPath + docConstants.TocAndFileUrlSeparator : '';
      path += pathInfo.contentPath ? pathInfo.contentPath : docConstants.TocFile;
      return path;
    };

    this.getContentUrlWithTocAndContentUrl = function(tocPath, contentPath) {
      var path = tocPath ? tocPath + docConstants.TocAndFileUrlSeparator : '';
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