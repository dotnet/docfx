// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
(function(){"use strict";
/* jshint browser: true */
/* global importScripts, onmessage: true, postMessage, lunr */

// Load up the lunr library
importScripts('lunr.min.js');

// Create the lunr index - the docs should be an array of object, each object containing
// the path and search terms for a page
var index = lunr(function() {
  this.ref('path');
  this.field('title', {boost: 50});
  this.field('keywords', { boost : 20 });
});

// Retrieve the searchData which contains the information about each page to be indexed
var searchData = {};
var searchDataRequest = new XMLHttpRequest();
searchDataRequest.onload = function() {

  // Store the pages data to be used in mapping query results back to pages
  searchData = JSON.parse(this.responseText);
  // Add search terms from each page to the search index
  for (var prop in  searchData) {
    index.add(searchData[prop]);
  }
  postMessage({ e: 'index-ready' });
};
searchDataRequest.open('GET', '../search-data.json');
searchDataRequest.send();

// The worker receives a message everytime the web app wants to query the index
onmessage = function(oEvent) {
  var q = oEvent.data.q
  ;var hits = index.search(q);
  var results = [];
  // Only return the array of paths to pages
  hits.forEach(function(hit) {
    var item = searchData[hit.ref];
    results.push({'path': item.path, 'display': item.display});
  });
  // The results of the query are sent back to the web app via a new message
  postMessage({ e: 'query-ready', q: q, d: results });
};
})();