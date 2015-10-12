function transform(model, _attrs){
  var entity = JSON.parse(model);
  var attrs = JSON.parse(_attrs);

  var vm = {};
  // Copy default attrs and override name/id
  for (var key in attrs) {
    if (attrs.hasOwnProperty(key)) {
      vm[key] = attrs[key];
    }
  }
  // Copy entity
  for (var key in entity) {
    if (entity.hasOwnProperty(key)) {
      vm[key] = entity[key];
    }
  }
  // If toc is not defined in model, read it from _attrs
  if (vm._tocPath && vm._tocPath.indexOf("~/") == 0){
    vm._tocPath = vm._tocPath.substring(2);
  }
  vm.conceptual = undefined;
  return {
    content: JSON.stringify(vm)
  };
}
