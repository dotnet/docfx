function transform(model, _attrs){
  var level = 1;
  var length = model.length;
  for (var i = 0; i<length; i++) {
    transformItem(model[i], level);
  };

  return {
    content: model
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
