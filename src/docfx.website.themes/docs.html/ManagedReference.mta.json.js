function transform(model, _attrs){
  model.layout = "Reference";
  model.title = model.items[0].name + " " + model.items[0].type;

  // If toc is not defined in model, read it from __attrs
  if (_attrs._tocPath && _attrs._tocPath.indexOf("~/") == 0){
    _attrs._tocPath = _attrs._tocPath.substring(2);
  }
  if (!model.toc_asset_id){
    model.toc_asset_id = _attrs._tocPath;
  }

  model.toc_rel = _attrs._tocRel;
  model.platforms = model.items[0].platform;
  model.langs = model.items[0].langs;
  if (!model.metadata || !model.metadata.breadcrumb_path) {
    model.breadcrumb_path = "/toc.html";
  } else {
    model.breadcrumb_path = model.metadata.breadcrumb_path
  }
  model.content_git_url = getImproveTheDocHref(model.items[0]);
  model.source_url = getViewSourceHref(model.items[0]);

 // Clean up unused predefined properties
  model.items = undefined;
  model.references = undefined;
  model.metadata = undefined;

  return {
    content: JSON.stringify(model)
  };

  function getImproveTheDocHref(item) {
    if (!item || !item.documentation || !item.documentation.remote) return '';
    return getRemoteUrl(item.documentation.remote, item.documentation.startLine + 1);
  }

  function getViewSourceHref(item) {
    /* jshint validthis: true */
    if (!item || !item.source || !item.source.remote) return '';
    return getRemoteUrl(item.source.remote, item.source.startLine - '0' + 1);
  }

  function getRemoteUrl(remote, startLine) {
    if (remote && remote.repo) {
      var repo = remote.repo;
      if (repo.substr(-4) === '.git') {
        repo = repo.substr(0, repo.length - 4);
      }
      var linenum = startLine ? startLine : 0;
      if (repo.match(/https:\/\/.*\.visualstudio\.com\/.*/g)) {
        // TODO: line not working for vso
        return repo + '#path=/' + remote.path;
      }
      if (repo.match(/https:\/\/.*github\.com\/.*/g)) {
        var path = repo + '/blob' + '/' + remote.branch + '/' + remote.path;
        if (linenum > 0) path += '/#L' + linenum;
        return path;
      }
      if (repo.match(/git@.*github\.com:.*/g)) {
        var path = 'https://' + repo.substr(4).replace(':', '/') + '/blob' + '/' + remote.branch + '/' + remote.path;
        if (linenum > 0) path += '/#L' + linenum;
        return path;
      }
    } else {
      return '';
    }
  }
}
