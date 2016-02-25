// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
function transform(model, _attrs) {
  var vm = {};
  // Copy default _attrs and override name/id
  for (var key in _attrs) {
    if (_attrs.hasOwnProperty(key)) {
      vm[key] = _attrs[key];
    }
  }

  var level = 1;
  var length = model.length;
  for (var i = 0; i < length; i++) {
    transformItem(model[i], level + 1);
  };
  vm.items = model;
  vm.level = 1;
  if (length > 0) vm.leaf = false;
  vm.title = "Table of Content";
  return vm;

  function transformItem(item, level) {
    item.level = level;
    if (item.items && item.items.length > 0) {
      var length = item.items.length;
      for (var i = 0; i < length; i++) {
        transformItem(item.items[i], level + 1);
      };
    } else {
      item.items = [];
      item.leaf = true;
    }
  }
}
