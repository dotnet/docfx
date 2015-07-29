// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
(function() {
  'use strict';

  angular.module('docascode.searchService', [])
  .provider('docsSearch', docsSearch);

  function docsSearch() {
    // This version of the service builds the index in the current thread,
    // which blocks rendering and other browser activities.
    // It should only be used where the browser does not support WebWorkers
    function localSearchFactory($http, $timeout) {

      console.log('Using Local Search Index');

      // Create the lunr index
      /*global lunr*/
      var index = lunr(function() {
        this.ref('path');
        this.field('title', {boost: 50});
        this.field('keywords', { boost : 20 });
      });

      var searchData = {};
      // Delay building the index by loading the data asynchronously
      var indexReadyPromise = $http.get('search-data.json').then(function(response) {
        searchData = response.data;
        // Delay building the index for 500ms to allow the page to render
        return $timeout(function() {
          // load the page data into the index
          for (var prop in searchData) {
            index.add(searchData[prop]);
          }
        }, 500);
      });

      // The actual service is a function that takes a query string and
      // returns a promise to the search results
      // (In this case we just resolve the promise immediately as it is not
      // inherently an async process)
      return function(q) {
        return indexReadyPromise.then(function() {
          var hits = index.search(q);
          var results = [];
          angular.forEach(hits, function(hit) {
            var item = searchData[hit.ref];
            results.push({'path': item.path, 'display': item.display});
          });
          return results;
        });
      };
    }
    localSearchFactory.$inject = ['$http', '$timeout'];

    // This version of the service builds the index in a WebWorker,
    // which does not block rendering and other browser activities.
    // It should only be used where the browser does support WebWorkers
    function webWorkerSearchFactory($q, $rootScope) {

      console.log('Using WebWorker Search Index');

      var searchIndex = $q.defer();
      var results;

      var worker = new Worker('scripts/search-worker.js');

      // The worker will send us a message in two situations:
      // - when the index has been built, ready to run a query
      // - when it has completed a search query and the results are available
      worker.onmessage = function(oEvent) {
        $rootScope.$apply(function() {

          switch(oEvent.data.e) {
            case 'index-ready':
              searchIndex.resolve();
              break;
            case 'query-ready':
              var pages = oEvent.data.d.map(function(item) {
                return item;
              });
              results.resolve(pages);
              break;
          }
        });
      };

      // The actual service is a function that takes a query string and
      // returns a promise to the search results
      return function(q) {

        // We only run the query once the index is ready
        return searchIndex.promise.then(function() {

          results = $q.defer();
          worker.postMessage({ q: q });
          return results.promise;
        });
      };
    }
    webWorkerSearchFactory.$inject = ['$q', '$rootScope'];

    return {
      $get: window.Worker ? webWorkerSearchFactory : localSearchFactory
    };
  }

})();