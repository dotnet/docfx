// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

var mrefCommon = require('./ManagedReference.common.js');
var extension = require('./ManagedReference.extension.js');
var overwrite = require('./ManagedReference.overwrite.js');

exports.transform = function (model) {
  if (overwrite && overwrite.transform) {
    return overwrite.transform(model);
  }

  if (extension && extension.preTransform) {
    model = extension.preTransform(model);
  }

  if (mrefCommon && mrefCommon.transform) {
    model = mrefCommon.transform(model);
  }
  if (model.type.toLowerCase() === "enum") {
    model.isClass = false;
    model.isEnum = true;
  }

  model._navRel = model._navRel === model._tocRel ? undefined : model._navRel;
  model._disableToc = model._disableToc || !model._tocPath;
  
  if (extension && extension.postTransform) {
    model = extension.postTransform(model);
  }

  return model;
}

exports.getOptions = function (model) {
  if (overwrite && overwrite.getOptions) {
    return overwrite.getOptions(model);
  }

  return {
    "bookmarks": mrefCommon.getBookmarks(model)
  };
}