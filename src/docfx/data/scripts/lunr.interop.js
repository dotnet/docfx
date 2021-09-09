// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

var lunr = require('./lunr.js');

exports.transform = function (documents) {
    var index = lunr(function () {
        this.field('title');
        this.field('body');

        documents.forEach(function (doc) {
            this.add(doc);
        }, this);
    });

    return JSON.stringify(index);
}
