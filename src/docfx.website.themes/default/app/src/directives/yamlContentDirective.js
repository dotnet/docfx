// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
/**
 * @ngdoc directive
 * @name docascode.directive:yamlContent
 * @restrict E
 * @element ANY
 * @priority 1000
 * @scope
 * 
 * @description yamlContent directive to help render markdown pages
 **/

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
    
    function getSyntax(element, language) {
      if (!element) return null;
      return utility.getNameWithSelector("content", language, element);
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
      $scope.getSyntax = getSyntax;
    }

    YamlContentController.$inject = ['$scope'];
    return {
      restrict: 'E',
      replace: true,
      templateUrl: 'views/yamlContent.html',
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