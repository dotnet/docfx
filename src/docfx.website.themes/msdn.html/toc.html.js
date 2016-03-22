// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
function transform(model, _attrs){
  transformItem(model, 0);
  return {
    content: model.items
  }

  function transformItem(item, level){
    item.level = level;
    if (item.items && item.items.length > 0){
      var length = item.items.length;
      for (var i = 0; i<length; i++) {
        transformItem(item.items[i], level + 1);
      };
    } else {
      item.items = [];
    }
  }
}
