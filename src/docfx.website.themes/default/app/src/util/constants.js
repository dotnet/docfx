/*
 * Define constants used in docsApp
 * Wrap Angular components in an Immediately Invoked Function Expression (IIFE)
 * to avoid variable collisions
*/

(function() {
    'use strict';
     /*jshint validthis:true */
    function provider() {
        this.YamlExtension = '.yml';
        this.MdExtension = '.md';
        this.TocYamlRegexExp = /toc\.yml$/;
        this.YamlRegexExp = /\.yml$/;
        this.MdRegexExp = /\.md$/;
        this.MdOrYamlRegexExp = /(\.yml$)|(\.md$)/;
        this.MdIndexFile = '.map';
        this.TocFile = 'toc' + this.YamlExtension; // docConstants.TocFile
        this.TocAndFileUrlSeperator = '!'; // docConstants.TocAndFileUrlSeperator
    }

    angular.module('docascode.constants', [])
        .service('docConstants', provider);

})();