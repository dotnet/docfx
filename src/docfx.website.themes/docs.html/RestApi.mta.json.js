// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
var common = require('./common.js');
var opCommon = require('./op.common.js');

function transform(model, _attrs) {
  model.title = model.name;
  model.layout = model.layout || "Rest";
  model.pagetype = "REST";
  model.langs = model.langs || ["http"];

  model.toc_asset_id = model.toc_asset_id || _attrs._tocPath;
  model.toc_rel = model.toc_rel || _attrs._tocRel;

  model.breadcrumb_path = model.breadcrumb_path || "/toc.html";
  model.content_git_url = model.content_git_url || common.getImproveTheDocHref(model, model.newFileRepository);

  var canonicalUrl;
  if (model._op_canonicalUrlPrefix && _attrs._path) {
    canonicalUrl = opCommon.getCanonicalUrl(model._op_canonicalUrlPrefix, _attrs._path);
  }

  // Clean up unused predefined properties
  var resetKeys = [
    "children",
    "htmlId",
    "schemes",
    "source",
    "swagger",
    "name",
    "uid",
    "newFileRepository",
    "documentation",
    "conceptual",
    "sections"
  ];

  model = opCommon.resetKeysAndSystemAttributes(model, resetKeys);
  model._op_canonicalUrl = canonicalUrl;
  return {
    content: JSON.stringify(model)
  };
}