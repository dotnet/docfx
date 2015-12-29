function transform(model, _attrs){
  var entityRaw = JSON.parse(model);
  var attrs = JSON.parse(_attrs);
  var entity = {};

  // Clean up unused predefined properties

  // If toc is not defined in model, read it from _attrs
  if (attrs._tocPath && attrs._tocPath.indexOf("~/") == 0){
    attrs._tocPath = attrs._tocPath.substring(2);
  }
  if (!entity.toc_asset_id){
    entity.toc_asset_id = attrs._tocPath;
  }
  if (attrs._navPath && attrs._navPath.indexOf("~/") == 0){
    attrs._navPath = attrs._navPath.substring(2);
  }
  if (!entity.breadcrumb_path){
    entity.breadcrumb_path = attrs._navPath;
  } 
  
  entity.langs = entityRaw.items[0].langs;
  entity.breadcrum_path = attrs._navRel;
  entity.doc_url =  getImproveTheDocHref(entityRaw.items[0]);
  entity.source_url = getViewSourceHref(entityRaw.items[0]);

  return {
    content: JSON.stringify(entity)
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
