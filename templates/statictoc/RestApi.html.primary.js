// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var restApiCommon = require('./RestApi.common.js');
var extension = require('./RestApi.extension.js')
var util = require('./statictoc.util.js');

exports.transform = function (model) {
  if (extension && extension.preTransform) {
    model = extension.preTransform(model);
  }

  if (restApiCommon && restApiCommon.transform) {
    model = restApiCommon.transform(model);
  }
  model._disableToc = model._disableToc || !model._tocPath || (model._navPath === model._tocPath);
  model = util.setToc(model);

  if (extension && extension.postTransform) {
    model = extension.postTransform(model);
  }

  return model;
}

exports.getOptions = function (model) {
  return { "bookmarks": restApiCommon.getBookmarks(model) };
}
