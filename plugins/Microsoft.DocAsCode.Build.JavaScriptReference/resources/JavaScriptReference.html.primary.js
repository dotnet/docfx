// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

var jsrefCommon = require('./JavaScriptReference.common.js');

exports.transform = function (model) {
  model = jsrefCommon.transform(model);
  if (model.type.toLowerCase() === "enum") {
    model.isClass = false;
    model.isEnum = true;
  }
  model._disableToc = model._disableToc || !model._tocPath || (model._navPath === model._tocPath);

  return { item: model };
}

exports.getOptions = function (model) {
  return { "bookmarks": jsrefCommon.getBookmarks(model) };
}