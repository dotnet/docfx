function transform(model, _attrs) {
  var entity = JSON.parse(model);
  var attrs = JSON.parse(_attrs);

  var vm = {};
  // Copy default attrs and override name/id
  for (var key in attrs) {
    if (attrs.hasOwnProperty(key)) {
      vm[key] = attrs[key];
    }
  }

  var level = 1;
  var length = entity.length;
  for (var i = 0; i < length; i++) {
    transformItem(entity[i], level + 1);
  };
  vm.items = entity;
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
