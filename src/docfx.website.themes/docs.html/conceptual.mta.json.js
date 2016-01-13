function transform(model, _attrs){
  var entity = JSON.parse(model);
  var attrs = JSON.parse(_attrs);

  entity.layout = "Conceptual";

  // Clean up unused predefined properties
  entity.conceptual = undefined;
  entity.remote = undefined;
  entity.path = undefined;
  entity.type = undefined;
  entity.source = undefined;

  // If toc is not defined in model, read it from _attrs
  if (attrs._tocPath && attrs._tocPath.indexOf("~/") == 0){
    attrs._tocPath = attrs._tocPath.substring(2);
  }
  if (!entity.toc_asset_id){
    entity.toc_asset_id = attrs._tocPath;
  }
  entity.toc_rel = attrs._tocRel;
  if (!entity.breadcrumb_path){
    entity.breadcrumb_path = "/toc.html";
  }
  return {
    content: JSON.stringify(entity)
  };
}
