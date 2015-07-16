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

(function () {
  'use strict';

  angular.module('docascode.controller')
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