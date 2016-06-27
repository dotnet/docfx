// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

var mrefCommon = require('./ManagedReference.common.js');

exports.transform = function (model) {
  model = mrefCommon.transform(model);
  if (model.type.toLowerCase() == "enum") {
    model.isClass = false;
    model.isEnum = true;
  }

  // Add "platform" to group of children. The value is the union of children's "platform".
  // Rendering can use this key to decide whether to show the subtitle of a group(e.g., Methods/Properties...) after switching platform.
  if (model.children && model.platform) {
    model.children.forEach(function (group) { addGroupPlatform(group); })
  }

  model._disableToc = model._disableToc || !model._tocPath || (model._navPath === model._tocPath);
  return { item: model };

  function addGroupPlatform(group) {
    if (!group || !group.children || group.children.length == 0) return;

    var platform = [];
    for (var i = 0; i < group.children.length; i++) {
      platform = union(platform, group.children[i].platform)
    }
    group.platform = platform;
  }

  function union(a, b) {
    if (!a) return b;
    if (!b) return a;
    return a.concat(b.filter(function (item) {
      return a.indexOf(item) < 0;
    }))
  }
}