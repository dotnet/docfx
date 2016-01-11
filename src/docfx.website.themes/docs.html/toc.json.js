function transform(model, _attrs){
  var entity = JSON.parse(model);
  var transformed = [];
  var level = 1;
  var length = entity.length;
  for (var i = 0; i<length; i++) {
    transformed.push(transformItem(entity[i], level));
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
        if (item.href.indexOf('~/') == 0) item.href = item.href.substring(2);
        item.href = removeExtension(item.href);
      }
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

  function removeExtension(path){
    var index = path.lastIndexOf('.');
    if (index > 0){
      return path.substring(0, index);
    }
    return path;
  }
}
