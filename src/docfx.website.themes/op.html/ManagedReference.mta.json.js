// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

var common = require('./common.js');
var opCommon = require('./op.common.js');

function transform(model, _attrs) {
  model.pagetype = "Reference";

  // If toc is not defined in model, read it from __attrs
  if (_attrs._tocPath && _attrs._tocPath.indexOf("~/") == 0) {
    _attrs._tocPath = _attrs._tocPath.substring(2);
  }

  model.toc_asset_id = model.toc_asset_id || _attrs._tocPath;

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
    "rawTitle"
  ];

  model = opCommon.resetKeysAndSystemAttributes(model, resetKeys);

  return {
    content: JSON.stringify(model)
  };
}
