/* global angular */
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
        var codeSnippet = utility.substringLine(result.data, startLine, endLine, true);
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