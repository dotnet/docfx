// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

var restApiCommon = require('./RestApi.common.js');

exports.transform = function (model) {
  model = restApiCommon.transform(model);
  model._disableToc = model._disableToc || !model._tocPath || (model._navPath === model._tocPath);
  return model;
}
