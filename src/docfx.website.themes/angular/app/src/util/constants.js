// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
        this.TocAndFileUrlSeparator = '!'; // docConstants.TocAndFileUrlSeparator
    }

    angular.module('docascode.constants', [])
        .service('docConstants', provider);

})();