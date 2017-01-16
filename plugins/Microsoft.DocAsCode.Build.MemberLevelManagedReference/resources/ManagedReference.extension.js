// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
var common = require('./ManagedReference.common.js');

exports.postTransform = function (model) {
    var type = model.type;
    var typePropertyName = common.getTypePropertyName(type);
    if (!typePropertyName) {
        console.error("Type " + model.type + " is not supported.");
        return;
    }

    model.isCollection = false;
    model.isItem = false;

    if (model.children && model.children.length > 1) {
        model.isCollection = true;
        common.groupChildren(model, 'class');
    } else {
        model.isItem = true;
    }
    return model;
}