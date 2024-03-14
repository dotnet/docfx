// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var urefCommon = require('./UniversalReference.common.js');
var extension = require('./UniversalReference.extension.js');

exports.transform = function (model) {
  if (extension && extension.preTransform) {
    model = extension.preTransform(model);
  }

  if (urefCommon && urefCommon.transform) {
    model = urefCommon.transform(model);
  }

  if(model._disableToc === undefined) {
    model._disableToc = model._disableToc || !model._tocPath || (model._navPath === model._tocPath);
  }
  model._disableNextArticle = true;

  if (extension && extension.postTransform) {
    model = extension.postTransform(model);
  }

  return model;
}

exports.getOptions = function (model) {
  return {
    "bookmarks": urefCommon.getBookmarks(model)
  };
}
