// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
function transform(model, _attrs) {
  model.layout = model.layout || "Conceptual";
  model.pagetype = "Conceptual";

  if (model._op_canonicalUrlPrefix && _attrs._path) {
    model._op_canonicalUrl = getCanonicalUrl(model._op_canonicalUrlPrefix, _attrs._path);
  }

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

  model.toc_asset_id = model.toc_asset_id || _attrs._tocPath;
  model.toc_rel = _attrs._tocRel;
  model.breadcrumb_path = model.breadcrumb_path || "/toc.html";

  return {
    content: JSON.stringify(model)
  };

  function getCanonicalUrl(canonicalUrlPrefix, path) {
    if (!canonicalUrlPrefix || !path) return '';
    if (canonicalUrlPrefix[canonicalUrlPrefix.length - 1] == '/')
    {
        canonicalUrlPrefix = canonicalUrlPrefix.slice(0, -1);
    }
    return canonicalUrlPrefix + "/" + removeExtension(path);
  }

  function removeExtension(path){
    var index = path.lastIndexOf('.');
    if (index > 0){
      return path.substring(0, index);
    }
    return path;
  }
}
