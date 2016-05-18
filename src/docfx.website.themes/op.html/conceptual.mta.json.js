// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
var opCommon = require('./op.common.js');

exports.transform = function (model) {
  model.pagetype = "Conceptual";
  model.toc_asset_id = model.toc_asset_id || model._tocPath;

  var resetKeys = [
    "conceptual",
    "remote",
    "path",
    "type",
    "source",
    "rawTitle",
    "wordCount",
    "newFileRepository"
  ];

  model = opCommon.resetKeysAndSystemAttributes(model, resetKeys, false);

  return {
    content: JSON.stringify(model)
  };
}
