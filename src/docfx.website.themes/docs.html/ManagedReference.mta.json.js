// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
var common = require('./common.js');
var opCommon = require('./op.common.js');

exports.transform = function (model) {
  model.layout = model.layout || "Reference";
  model.pagetype = "Reference";
  model.title = model.title || (model.name[0].value + " " + model.type);

  model.toc_asset_id = model.toc_asset_id || model._tocPath;
  model.toc_rel = model.toc_rel || model._tocRel;

  model.platforms = model.platforms || model.platform;
  model.breadcrumb_path = model.breadcrumb_path || "/toc.html";
  model.content_git_url = model.content_git_url || common.getImproveTheDocHref(model, model.newFileRepository);
  model.source_url = model.source_url || common.getViewSourceHref(model);
  model.asset_id = model.asset_id || opCommon.getAssetId(model);

  if (model.langs && model.display_langs && model.display_langs.length > 0) {
    model.display_langs = toLowerCase(model.display_langs);
    model.langs = intersection(model.langs, model.display_langs);
  }

  var canonicalUrl;
  if (model._op_canonicalUrlPrefix && model._path) {
    canonicalUrl = opCommon.getCanonicalUrl(model._op_canonicalUrlPrefix, model._path, model.layout);
  }
  model.canonical_url = canonicalUrl;

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
    "requirements",
    "isEii",
    "isExtensionMethod",
    "nameWithType",
    "extensionMethods"
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

function intersection(arr1, arr2) {
  return arr1.filter(function (item) {
    return arr2.indexOf(item) != -1;
  });
}

function toLowerCase(arr) {
  return arr.map(function (item) {
    return item.toLowerCase();
  });
}
