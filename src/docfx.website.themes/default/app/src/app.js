/* 
 * https://github.com/johnpapa/angular-styleguide
 * define 1 component per file
*/
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

angular.module('docascode.controller', ['docascode.contentService', 'docascode.urlService', 'docascode.directives', 'docascode.util', 'docascode.constants', 'docascode.searchService']);

angular.module('docascode.directives', ['itemTypes', 'docascode.urlService', 'docascode.util', 'docascode.markdownService','docascode.mapfileService', 'docascode.csplayService', 'docascode.contentService']);
