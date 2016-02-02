function transform(model, _attrs){
  // Clean up unused predefined properties
  model.conceptual = undefined;
  model.remote = undefined;
  model.path = undefined;
  model.type = undefined;
  model.source = undefined;
  model.rawTitle = undefined;

  // Clean up open publishing internal used properties
  model._op_accessToken = undefined;
  model._op_clientId = undefined;
  model._op_clientSecret = undefined;
  model._op_gitContributorInformation = undefined;
  model.newFileRepository = undefined;

  if (!model.toc_asset_id){
    model.toc_asset_id = _attrs._tocPath;
  }
  return {
    content: JSON.stringify(model)
  };
}
