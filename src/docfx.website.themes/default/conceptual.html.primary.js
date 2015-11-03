function transform(model, _attrs){
  var entity = JSON.parse(model);
  var attrs = JSON.parse(_attrs);

  var vm = {};
  // Copy default attrs and override name/id
  for (var key in attrs) {
    if (attrs.hasOwnProperty(key)) {
      vm[key] = attrs[key];
    }
  }
  // Copy entity
  for (var key in entity) {
    if (entity.hasOwnProperty(key)) {
      vm[key] = entity[key];
    }
  }
  // If toc is not defined in model, read it from _attrs
  if (vm._tocPath && vm._tocPath.indexOf("~/") == 0){
    vm._tocPath = vm._tocPath.substring(2);
  }
  if (vm._navPath === vm._tocPath){
    vm._allowToc = false;
  }else{
    vm._allowToc = true;
  }
  vm.docurl = getImproveTheDocHref(vm);
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
    } else {
      return '';
    }
  }
}
