(function() {
  importScripts('lunr.min.js');

  var lunrIndex = lunr(function() {
      this.ref('href');
      this.field('title', {boost: 50});
      this.field('keywords', {boost: 20});
  });
  lunr.tokenizer.seperator = /[\s\-\.]+/;
  var searchData = {};
  var searchDataRequest = new XMLHttpRequest();

  searchDataRequest.open('GET', '../index.json');
  searchDataRequest.onload = function() {
    searchData = JSON.parse(this.responseText);
    for (var prop in searchData) {
      lunrIndex.add(searchData[prop]);
    }
    postMessage({e: 'index-ready'});
  }
  searchDataRequest.send();

  onmessage = function(oEvent) {
    var q = oEvent.data.q;
    var hits = lunrIndex.search(q);
    var results = [];
    hits.forEach(function(hit) {
      var item = searchData[hit.ref];
      results.push({'href': item.href, 'title': item.title, 'keywords': item.keywords});
    });
    postMessage({e: 'query-ready', q: q, d: results});
  }
})();
