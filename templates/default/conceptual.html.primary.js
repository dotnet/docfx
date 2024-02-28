// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var common = require('./common.js');
var extension = require('./conceptual.extension.js')

exports.transform = function (model) {
  if (extension && extension.preTransform) {
    model = extension.preTransform(model);
  }

  if(model._disableToc === undefined) {
    model._disableToc = model._disableToc || !model._tocPath || (model._navPath === model._tocPath);
  }
  model.docurl = model.docurl || common.getImproveTheDocHref(model, model._gitContribute, model._gitUrlPattern);

  if (extension && extension.postTransform) {
    model = extension.postTransform(model);
  }

  return model;
}
