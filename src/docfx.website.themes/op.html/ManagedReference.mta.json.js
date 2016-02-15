function transform(model, _attrs){
  // If toc is not defined in model, read it from __attrs
  if (_attrs._tocPath && _attrs._tocPath.indexOf("~/") == 0){
    _attrs._tocPath = _attrs._tocPath.substring(2);
  }
  if (!model.toc_asset_id){
    model.toc_asset_id = _attrs._tocPath;
  }

  // Clean up unused predefined properties
  model.items = undefined;
  model.references = undefined;
  model.newFileRepository = undefined;

  return {
    content: JSON.stringify(model)
  };
}
