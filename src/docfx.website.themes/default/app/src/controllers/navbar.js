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