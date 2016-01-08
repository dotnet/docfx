function transform(model, _attrs){
  var entity = JSON.parse(model);
  var attrs = JSON.parse(_attrs);

  // Clean up unused predefined properties
  entity.conceptual = undefined;
  entity.remote = undefined;
  entity.path = undefined;
  entity.type = undefined;
  entity.source = undefined;
  entity.articleTitleHtml = undefined;
  entity.articleContentHtml = undefined;

  // Clean up open publishing internal used properties
  entity._op_accessToken = undefined;
  entity._op_clientId = undefined;
  entity._op_clientSecret = undefined;
  entity._op_gitContributorInformation = undefined;

  // If toc is not defined in model, read it from _attrs
  if (attrs._tocPath && attrs._tocPath.indexOf("~/") == 0){
    attrs._tocPath = attrs._tocPath.substring(2);
  }
  if (!entity.toc_asset_id){
    entity.toc_asset_id = attrs._tocPath;
  }
  return {
    content: JSON.stringify(entity)
  };
}
