// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

var core = require('./ManagedReference.html.primary.core.js');

exports.transform = function (model) {
  model = core.transform(model);
  return {item: model};
}