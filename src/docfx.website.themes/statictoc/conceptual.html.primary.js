// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
var common = require('./common.js');
var util = require('./statictoc.util.js');

exports.transform = function (model) {
  model._disableToc = model._disableToc || !model._tocPath || (model._navPath === model._tocPath);
  model.docurl = model.docurl || common.getImproveTheDocHref(model, model._gitContribute, model._gitUrlPattern);
  model = util.setToc(model);
  return model;
}
