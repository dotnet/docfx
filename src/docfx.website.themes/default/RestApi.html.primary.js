// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

var restApiCommon = require('./RestApi.common.js');
var extension = require('./RestApi.extension.js')

exports.transform = function (model) {
  model = extension.preSteps(model);

  model = restApiCommon.transform(model);
  model._disableToc = model._disableToc || !model._tocPath || (model._navPath === model._tocPath);

  model = extension.postSteps(model);

  return model;
}
