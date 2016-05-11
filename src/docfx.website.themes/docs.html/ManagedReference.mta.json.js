// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
var common = require('./common.js');
var opCommon = require('./op.common.js');

function transform(model, _attrs) {
  model.layout = model.layout || "Reference";
  model.pagetype = "Reference";
  if (!model.title) {
    model.title = model.name[0].value + " " + model.type;
  }

  // If toc is not defined in model, read it from __attrs
  if (_attrs._tocPath && _attrs._tocPath.indexOf("~/") == 0) {
    _attrs._tocPath = _attrs._tocPath.substring(2);
  }
  model.toc_asset_id = model.toc_asset_id || _attrs._tocPath;
  model.toc_rel = model.toc_rel || _attrs._tocRel;

  model.platforms = model.platforms || model.platform;
  model.breadcrumb_path = model.breadcrumb_path || "/toc.html";
  model.content_git_url = model.content_git_url || common.getImproveTheDocHref(model, model.newFileRepository);
  model.source_url = model.source_url || common.getViewSourceHref(model);
  model.asset_id = model.asset_id || opCommon.getAssetId(model);
  
  var canonicalUrl;
  if (model._op_canonicalUrlPrefix && _attrs._path) {
    canonicalUrl = opCommon.getCanonicalUrl(model._op_canonicalUrlPrefix, _attrs._path);
  }

  // Clean up unused predefined properties
  var resetKeys = [
    "uid",
    "id",
    "parent",
    "children",
    "href",
    "name",
    "fullName",
    "type",
    "source",
    "documentation",
    "assemblies",
    "namespace",
    "summary",
    "remarks",
    "example",
    "syntax",
    "overridden",
    "exceptions",
    "seealso",
    "see",
    "inheritance",
    "level",
    "implements",
    "inheritedMembers",
    "conceptual",
    "platform",
    "newFileRepository",
    "thread_safety",
    "defined_in",
    "supported_platforms",
    "requirements"
  ];

  model = opCommon.resetKeysAndSystemAttributes(model, resetKeys);

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
