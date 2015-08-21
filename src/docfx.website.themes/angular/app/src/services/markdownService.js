// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
/* global marked */
/* global hljs */
/*
 * Define content load related functions used in docsController
 * Wrap Angular components in an Immediately Invoked Function Expression (IIFE)
 * to avoid variable collisions
 */

(function () {
  'use strict';
  /*jshint validthis:true */
  function provider($q, $http, $location, urlService, csplayService, contentService, docUtility) {
    this.transform = transform;
    this.updateHref = updateHref;
    var md = (function () {
      marked.setOptions({
        gfm: true,
        pedantic: false,
        sanitize: false, // DONOT sanitize html tags
      });

      var toHtml = function (markdown) {
        if (!markdown)
          return '';

        return marked(markdown);
      };
      return {
        toHtml: toHtml
      };
    } ());
    
    function updateHref(element, navItems){
      angular.forEach(element.find("a"), function (block) {
        var url = block.attributes['href'] && block.attributes['href'].value;
        if (!url) return;
        if (!urlService.isAbsoluteUrl(url))
          block.attributes['href'].value = urlService.getPageHref($location.path(), url, navItems);
      });
    }
    
    function transform(element, markdown, navItems) {
      var html = md.toHtml(markdown);
      element.html(html);
      angular.forEach(element.find("code"), function (block) {
        // use highlight.js to highlight code
        hljs.highlightBlock(block);
        // Add try button
        block = block.parentNode;
        if (angular.element(block).is("pre")) { //make sure the parent is pre and not inline code
          var wrapper = document.createElement("div");
          wrapper.className = "codewrapper";
          wrapper.innerHTML = '<div class="trydiv"><span class="tryspan">Try this code</span></div>';
          wrapper.childNodes[0].childNodes[0].onclick = function () {
            csplayService.tryCode(true, block.innerText);
          };
          block.parentNode.replaceChild(wrapper, block);
          wrapper.appendChild(block);
        }
      });
      updateHref(element, navItems);
      angular.forEach(element.find("img"), function (block) {
        var url = block.attributes['src'] && block.attributes['src'].value;
        if (!url) return;
        if (!urlService.isAbsoluteUrl(url))
          block.attributes['src'].value = urlService.getAbsolutePath($location.path(), url);
      });
      angular.forEach(element.find("table"), function (block) {
        angular.element(block).addClass('table table-bordered table-striped table-condensed');
      });
      angular.forEach(element.find("code-snippet", function (block) {
        var src = block.attributes['src'];
        var startLine = block.attributes['sl'];
        var endLine = block.attributes['el'];
        contentService.getMdContent(src).then(function(result){
          var data = result.data;
          var snippet = docUtility.substringLine(data, startLine, endLine);
          var wrapper = document.createElement("div");
          wrapper.innerHTML = snippet;
          block.parentNode.replaceChild(wrapper, block);
        });
      }));
      
      // Only return html() makes registered event, e.g. onclick missing
      // Instead, pass in element
    }
  }

  angular.module('docascode.markdownService', ['docascode.urlService', 'docascode.csplayService', 'docascode.contentService', 'docascode.util'])
    .service('markdownService', ['$q', '$http', '$location', 'urlService', 'csplayService', 'contentService', 'docUtility', provider]);

})();