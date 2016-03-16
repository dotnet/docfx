// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
function transform(model, _attrs){
  model.layout = "Reference";
  model.title = model.name[0].value + " " + model.type;

  // If toc is not defined in model, read it from __attrs
  if (_attrs._tocPath && _attrs._tocPath.indexOf("~/") == 0){
    _attrs._tocPath = _attrs._tocPath.substring(2);
  }
  if (!model.toc_asset_id){
    model.toc_asset_id = _attrs._tocPath;
  }

  model.toc_rel = _attrs._tocRel;
  model.platforms = model.platform;
  if (!model.breadcrumb_path) {
    model.breadcrumb_path = "/toc.html";
  }
  model.content_git_url = getContentGitUrl(model, model.newFileRepository);
  model.source_url = getViewSourceHref(model);
  model["ms.assetid"] = getMsAssetId(model);

  // Clean up unused predefined properties
  model.uid = undefined;
  model.id = undefined;
  model.parent = undefined;
  model.children = undefined;
  model.href = undefined;
  model.name = undefined;
  model.fullName = undefined;
  model.type = undefined;
  model.source = undefined;
  model.documentation = undefined;
  model.assemblies = undefined;
  model.namespace = undefined;
  model.summary = undefined;
  model.remarks = undefined;
  model.example = undefined;
  model.syntax = undefined;
  model.overridden = undefined;
  model.exceptions = undefined;
  model.seealso = undefined;
  model.see = undefined;
  model.inheritance = undefined;
  model.level = undefined;
  model.implements = undefined;
  model.inheritedMembers = undefined;
  model.conceptual = undefined;
  model.platform = undefined;
  model.newFileRepository = undefined;
  model.thread_safety = undefined;
  model.defined_in = undefined;
  model.supported_platforms = undefined;
  model.requirements = undefined;

  return {
    content: JSON.stringify(model)
  };

  function getContentGitUrl(item, newFileRepository) {
    if (!item) return '';
    if (!item.documentation || !item.documentation.remote) {
      return getNewFileUrl(item.uid, newFileRepository);
    } else {
      return getRemoteUrl(item.documentation.remote, item.documentation.startLine + 1);
    }
  }

  function getViewSourceHref(item) {
    /* jshint validthis: true */
    if (!item || !item.source || !item.source.remote) return '';
    return getRemoteUrl(item.source.remote, item.source.startLine - '0' + 1);
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
      if (/https:\/\/.*\.visualstudio\.com\/.*/gi.test(repo)) {
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
    var regex = /^(?:https?:\/\/)?(?:\S+\@)?(?:\S+\.)?(github\.com(?:\/|:).*)/gi;
    if (!regex.test(repo)) return '';
    return repo.replace(regex, function(match, p1, offset, string) {
      return 'https://' + p1.replace(':', '/');
    })
  }

  function getMsAssetId(item) {
    if (!item || !item.uid) return '';
    return item.uid;
  }
}
