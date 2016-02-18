function transform(model, _attrs) {
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
    vm.title = vm.title || vm.name;

    vm.docurl = vm.docurl || getImproveTheDocHref(vm, vm.newFileRepository);
    vm.sourceurl = vm.sourceurl || getViewSourceHref(vm);
    if (vm.children) {
        var ordered = [];
        for (var i = 0; i < vm.children.length; i++) {
            var child = vm.children[i];
            child.docurl = child.docurl || getImproveTheDocHref(child, vm.newFileRepository);
            child.sourceurl = child.sourceurl || getViewSourceHref(child);
            child.conceptual = child.conceptual || ''; // set to empty incase mustache looks up
            if (vm._displayItems && child.uid) {
                var index = vm._displayItems.indexOf(child.uid);
                if (index > -1) {
                    ordered[index] = child;
                }
            }
        };
        if (vm._displayItems) {
            vm.children = ordered;
        }
    }

    return vm;

    function getImproveTheDocHref(item, newFileRepository) {
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
        content += "description: You can override description for the API here using *MARKDOWN* syntax\n";
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
        if (repo.match(/https:\/\/.*github\.com\/.*/g)) {
            return repo;
        }
        if (repo.match(/git@.*github\.com:.*/g)) {
            return 'https://' + repo.substr(4).replace(':', '/');
        }
        return '';
    }
}
