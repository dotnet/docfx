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
  vm._disableToc = vm._disableToc || !vm._tocPath || (vm._navPath === vm._tocPath);
  vm.docurl = vm.docurl || getImproveTheDocHref(vm);
  return vm;


  function getImproveTheDocHref(item) {
    if (!item || !item.source || !item.source.remote) return '';
    return getRemoteUrl(item.source.remote, item.source.startLine + 1);
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
