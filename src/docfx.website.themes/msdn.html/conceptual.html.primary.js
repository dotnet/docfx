function transform(model, _attrs){
  var entity = JSON.parse(model);
  var attrs = JSON.parse(_attrs);

  // If toc is not defined in model, read it from _attrs
  if (attrs._tocPath && attrs._tocPath.indexOf("~/") == 0){
    attrs._tocPath = attrs._tocPath.substring(2);
  }
  entity._tocPath = attrs._tocPath;
  return entity;
}
