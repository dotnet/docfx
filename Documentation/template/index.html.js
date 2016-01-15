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
  // If toc is not defined in model, read it from _attrs
  if (vm._tocPath && vm._tocPath.indexOf("~/") == 0) {
    vm._tocPath = vm._tocPath.substring(2);
  }
  if (vm._navPath === vm._tocPath){
    vm._allowToc = false;
  }else{
    vm._allowToc = true;
  }
  if (!vm.hasOwnProperty("_allowAffix")) {
    vm._allowAffix = true;
  } else {
    // parse from string to bool
    vm._allowAffix = vm._allowAffix === "true"
  }
  return vm;
}
