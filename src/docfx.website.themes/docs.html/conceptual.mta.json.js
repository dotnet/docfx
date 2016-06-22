// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
var opCommon = require('./op.common.js');

exports.transform = function (model) {
  model.layout = model.layout || "Conceptual";
  model.pagetype = "Conceptual";

  var canonicalUrl;
  if (model._op_canonicalUrlPrefix && model._path) {
    canonicalUrl = opCommon.getCanonicalUrl(model._op_canonicalUrlPrefix, model._path, model.layout);
  }
  model.canonical_url = canonicalUrl;

  model.toc_asset_id = model.toc_asset_id || model._tocPath;
  model.toc_rel = model._tocRel;
  model.breadcrumb_path = model.breadcrumb_path || "/toc.html";

  // Clean up unused predefined properties
  var resetKeys = [
    "conceptual",
    "remote",
    "path",
    "type",
    "source",
    "newFileRepository",
    "baseRepositoryDirectory",
    "_displayLangs"
  ];
  model = opCommon.resetKeysAndSystemAttributes(model, resetKeys, true);

  // For metadata consumed by docs themes, rename with prefix "_op_"
  var metaForThemes = ["wordCount", "rawTitle"];
  for (var index = 0; index < metaForThemes.length; ++index) {
    var meta = metaForThemes[index];
    model["_op_".concat(meta)] = model[meta];
    model[meta] = undefined;
  }
  model._op_canonicalUrl = canonicalUrl;
  return {
    content: JSON.stringify(model)
  };
}
