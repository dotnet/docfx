// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

var common = require('./common.js');
var opCommon = require('./op.common.js');
exports.transform = function (model) {
  model.pagetype = "Reference";
  model.toc_asset_id = model.toc_asset_id || model._tocPath;

  model.content_git_url = model.content_git_url || common.getImproveTheDocHref(model, model.newFileRepository);

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
    "wordCount",
    "rawTitle",
    "isEii",
    "isExtensionMethod",
    "nameWithType",
    "extensionMethods"
  ];

  model = opCommon.resetKeysAndSystemAttributes(model, resetKeys, false);

  return {
    content: JSON.stringify(model)
  };
}
