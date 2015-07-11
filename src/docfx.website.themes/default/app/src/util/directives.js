/* global angular */
/*
 * Define directives used in docsApp
 * Wrap Angular components in an Immediately Invoked Function Expression (IIFE)
 * to avoid variable collisions
 */

(function () {
  'use strict';

  angular.module('docascode.directives')
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
    function render(element, content, language){
      element.text(content);
      if (language) element.addClass(language);
      angular.forEach(element, function (block) {
        hljs.highlightBlock(block, language);
      });
    }
    
    return {
      restrict: 'E',
      terminal: true,
      scope: {
        language: "=ngLanguage",
        data: "=data"
      },
      link: function (scope, element, attrs) {
        var unwatchData = scope.$watch("data", function (value, oldValue) {
          if (value === undefined) return;
          render(element, value, scope.language);
          unwatchData();
        });
        var unwatchLang = scope.$watch("language", function (value, oldValue) {
          if (value === undefined) return;
          render(element, scope.data, value);
          unwatchLang();
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