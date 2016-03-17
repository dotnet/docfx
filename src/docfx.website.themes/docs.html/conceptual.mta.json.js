// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
function transform(model, _attrs){
  model.layout = "Conceptual";

  // Clean up unused predefined properties
  model.conceptual = undefined;
  model.remote = undefined;
  model.path = undefined;
  model.type = undefined;
  model.source = undefined;
  model.newFileRepository = undefined;

  model._docfxVersion = undefined;

  // For metadata consumed by docs themes, rename with prefix "_op_"
  var metaForThemes = ["wordCount", "rawTitle"];
  for (var index = 0; index < metaForThemes.length; ++index) {
    var meta = metaForThemes[index];
    model["_op_".concat(meta)] = model[meta];
    model[meta] = undefined;
  }

  if (!model.toc_asset_id){
    model.toc_asset_id = _attrs._tocPath;
  }
  model.toc_rel = _attrs._tocRel;
  if (!model.breadcrumb_path){
    model.breadcrumb_path = "/toc.html";
  }
  return {
    content: JSON.stringify(model)
  };
}
