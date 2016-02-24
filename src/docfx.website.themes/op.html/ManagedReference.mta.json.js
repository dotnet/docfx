function transform(model, _attrs){
  // If toc is not defined in model, read it from __attrs
  if (_attrs._tocPath && _attrs._tocPath.indexOf("~/") == 0){
    _attrs._tocPath = _attrs._tocPath.substring(2);
  }
  if (!model.toc_asset_id){
    model.toc_asset_id = _attrs._tocPath;
  }

  model.content_git_url = getContentGitUrl(model.items[0], model.newFileRepository);

  // Clean up unused predefined properties
  model.items = undefined;
  model.references = undefined;
  model.newFileRepository = undefined;

  function getContentGitUrl(item, newFileRepository) {
    if (!item) return '';
    if (!item.documentation || !item.documentation.remote) {
      return getNewFileUrl(item.uid, newFileRepository);
    } else {
      return getRemoteUrl(item.documentation.remote, item.documentation.startLine + 1);
    }
  }

  function getNewFileUrl(uid, newFileRepository) {
    // do not support VSO for now
    if (newFileRepository && newFileRepository.repo) {
      var repo = newFileRepository.repo;
      if (repo.substr(-4) === '.git') {
        repo = repo.substr(0, repo.length - 4);
      }
      var path = getGithubUrlPrefix(repo);
      if (path != '') {
        path += '/new';
        path += '/' + newFileRepository.branch;
        path += '/' + getOverrideFolder(newFileRepository.path);
        path += '/new?filename=' + getHtmlId(uid) + '.md';
        path += '&value=' + encodeURIComponent(getOverrideTemplate(uid));
      }
      return path;
    } else {
      return '';
    }
  }

  function getOverrideFolder(path) {
    if (!path) return "";
    path = path.replace('\\', '/');
    if (path.charAt(path.length - 1) == '/') path = path.substring(0, path.length - 1);
    return path;
  }

  function getHtmlId(input) {
    return input.replace(/\W/g, '_');
  }

  function getOverrideTemplate(uid) {
    if (!uid) return "";
    var content = "";
    content += "---\n";
    content += "uid: " + uid + "\n";
    content += "remarks: '*THIS* is remarks overriden in *MARKDOWN* file'\n";
    content += "---\n";
    content += "\n";
    content += "*Please type below more information about this API:*\n";
    content += "\n";
    return content;
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
      var path = getGithubUrlPrefix(repo);
      if (path != '') {
        path += '/blob' + '/' + remote.branch + '/' + remote.path;
        if (linenum > 0) path += '/#L' + linenum;
      }
      return path;
    } else {
      return '';
    }
  }

  function getGithubUrlPrefix(repo) {
    if (repo.match(/https:\/\/(|\S+\.)github\.com\/.*/g)) {
      return repo;
    }
    if (repo.match(/git@(|\S+\.)github\.com:.*/g)) {
      return 'https://' + repo.substr(4).replace(':', '/');
    }
    return '';
  }

  return {
    content: JSON.stringify(model)
  };
}
