// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
var common = require('./common.js');
var opCommon = require('./op.common.js');

exports.transform = function (model) {
  model.title = model.name;
  model.layout = model.layout || "Rest";
  model.pagetype = "REST";
  model.langs = model.langs || ["http"];

  model.toc_asset_id = model.toc_asset_id || model._tocPath;
  model.toc_rel = model.toc_rel || model._tocRel;

  model.breadcrumb_path = model.breadcrumb_path || "/toc.html";
  model.content_git_url = model.content_git_url || common.getImproveTheDocHref(model, model.newFileRepository);

  var canonicalUrl;
  if (model._op_canonicalUrlPrefix && model._path) {
    canonicalUrl = opCommon.getCanonicalUrl(model._op_canonicalUrlPrefix, model._path, model.layout);
  }
  model.canonical_url = canonicalUrl;

  // Clean up unused predefined properties
  var resetKeys = [
    "_raw",
    "basePath",
    "children",
    "conceptual",
    "consumes",
    "definitions",
    "documentation",
    "externalDocs",
    "footer",
    "host",
    "htmlId",
    "info",
    "name",
    "newFileRepository",
    "parameters",
    "produces",
    "responses",
    "schemes",
    "sections",
    "security",
    "securityDefinitions",
    "source",
    "swagger",
    "tags",
    "uid"
  ];

  model = opCommon.resetKeysAndSystemAttributes(model, resetKeys, true);
  model._op_canonicalUrl = canonicalUrl;
  return {
    content: JSON.stringify(model)
  };
}