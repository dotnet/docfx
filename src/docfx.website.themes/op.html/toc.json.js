function transform(model, _attrs){
  var transformed = [];
  var level = 1;
  var length = model.length;
  var path = _attrs._path;
  var directory = "";
    var index = path.lastIndexOf('/');
    if (index > -1){
      directory = path.substr(0, index + 1); // keep '/'
    }

  for (var i = 0; i<length; i++) {
    transformed.push(transformItem(model[i], level));
  };

  return {
    content: JSON.stringify(transformed)
  };

  function transformItem(item, level){
    item.toc_title = item.name;
    item.name = undefined;
    item.level = level;
    if (item.href){
      if (isAbsolutePath(item.href)){
        item.external_link = item.href;
      }else{
        if (item.href.indexOf('/') == 0) {
          item.relative_path_in_depot = item.href;
        } else {
          item.relative_path_in_depot = directory + item.href;
        }
      }
      item.href = undefined;
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

  function isAbsolutePath(path){
    return /^(\w+:)?\/\//g.test(path);
  }
}
