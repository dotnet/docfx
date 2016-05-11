// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
exports.getCanonicalUrl = function (canonicalUrlPrefix, path) {
  if (!canonicalUrlPrefix || !path) return '';
  if (canonicalUrlPrefix[canonicalUrlPrefix.length - 1] == '/') {
    canonicalUrlPrefix = canonicalUrlPrefix.slice(0, -1);
  }
  return canonicalUrlPrefix + "/" + exports.removeExtension(path);
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

exports.resetKeysAndSystemAttributes = function (model, resetKeys){
  return exports.batchSetProperties(
    model,
    function (key) {
      return key.indexOf('_') === 0 || (exports.isArray(resetKeys) && resetKeys.indexOf(key) > -1);
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