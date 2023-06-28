// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
var common = require('./common.js');

exports.transform = function (model) {
  var transformed = [];
  var path = model._path;
  var directory = "";
  var index = path.lastIndexOf('/');
  if (index > -1){
    directory = path.substr(0, index + 1); // keep '/'
  }

  transformItem(model, 0);
  return {
    content: JSON.stringify(model.children)
  };

  function transformItem(item, level){
    item.toc_title = item.name;
    item.name = undefined;
    item.level = level;
    if (item.href){
      if (common.isAbsolutePath(item.href)){
        item.external_link = item.href;
      }else{
        if (item.href.indexOf('~/') == 0) item.href = item.href.substring(2);
        if (item.href.indexOf('/') == 0) {
          item.relative_path_in_depot = item.href;
        } else {
          item.relative_path_in_depot = directory + item.href;
        }
      }
    }
    else {
      item.href='#';
    }
    if (item.items && item.items.length > 0){
      var children = [];
      var length = item.items.length;
      for (var i = 0; i<length; i++) {
        children.push(transformItem(item.items[i], level+1));
      };
      item.children = children;
      item.items = undefined;
    }
    return item;
  }
}
