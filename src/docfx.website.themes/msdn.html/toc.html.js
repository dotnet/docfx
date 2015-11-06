function transform(model, _attrs){
  var entity = JSON.parse(model);
  var level = 1;
  var length = entity.length;
  for (var i = 0; i<length; i++) {
    transformItem(entity[i], level);
  };

  return {
    content: entity
  }

  function transformItem(item, level){
    item.level = level;
    if (item.items && item.items.length > 0){
      var length = item.items.length;
      for (var i = 0; i<length; i++) {
        transformItem(item.items[i], level+1);
      };
    } else {
      item.items = [];
    }
  }
}
