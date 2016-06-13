// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
exports.getCanonicalUrl = function (canonicalUrlPrefix, path, layout) {
  if (!canonicalUrlPrefix || !path) return '';
  if (canonicalUrlPrefix[canonicalUrlPrefix.length - 1] == '/') {
    canonicalUrlPrefix = canonicalUrlPrefix.slice(0, -1);
  }

  var canonicalUrl = canonicalUrlPrefix + "/" + exports.removeExtension(path);
  
  if (typeof(layout) !== "undefined" && layout === "HubPage")
  {
    if (canonicalUrl.toLowerCase().endsWith("index"))
    {
      canonicalUrl = canonicalUrl.slice(0, -5);
    }
  }

  return canonicalUrl;
}

exports.getAssetId = function (item) {
  if (!item || !item.uid) return '';
  return item.uid;
}

exports.removeExtension = function (path) {
  var index = path.lastIndexOf('.');
  if (index > 0) {
    return path.substring(0, index);
  }
  return path;
}

exports.resetKeysAndSystemAttributes = function (model, resetKeys, keepOpAttributes){
  return exports.batchSetProperties(
    model,
    function (key) {
      if (exports.isArray(resetKeys) && resetKeys.indexOf(key) > -1) {
        return true;
      }
      if (key.indexOf('_op_') === 0 && keepOpAttributes) {
        return false;
      }
      return key.indexOf('_') === 0;
    },
    undefined);
}

exports.batchSetProperties = function (model, keySelector, setter) {
  if (model === undefined || keySelector === undefined) return;
  for (var key in model) {
    if (keySelector(key) && model.hasOwnProperty(key)) {
      var element = model[key];
      if (setter === undefined) {
        model[key] = undefined;
      }else{
        model[key] = setter(key);
      }
    }
  }
  return model;
}

exports.isArray = function (input){
  return input && Array.isArray(input);
}