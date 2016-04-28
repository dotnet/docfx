// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
var common = require('./common.js');

function transform(model, _attrs){
  var vm = {};
  // Copy default _attrs and override name/id
  for (var key in _attrs) {
    if (_attrs.hasOwnProperty(key)) {
      vm[key] = _attrs[key];
    }
  }
  // Copy model
  for (var key in model) {
    if (model.hasOwnProperty(key)) {
      vm[key] = model[key];
    }
  }
  vm._disableToc = vm._disableToc || !vm._tocPath || (vm._navPath === vm._tocPath);
  vm.docurl = vm.docurl || common.getViewSourceHref(vm);
  return vm;
}
