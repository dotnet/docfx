// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
function transform(model, _attrs) {
    var vm = {};
    vm.title = model.name;
    vm.layout = model.layout || "Rest";
    vm.pagetype = "REST";
    vm.langs = model.langs || ["http"];

    vm.toc_asset_id = model.toc_asset_id || _attrs._tocPath;
    vm.toc_rel = model.toc_rel || _attrs._tocRel;

    vm.breadcrumb_path = model.breadcrumb_path || "/toc.html";
    vm.content_git_url = model.content_git_url || getContentGitUrl(model, model.newFileRepository);

    return {
        content: JSON.stringify(vm)
    };

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
        var regex = /^(?:https?:\/\/)?(?:\S+\@)?(?:\S+\.)?(github\.com(?:\/|:).*)/gi;
        if (!regex.test(repo)) return '';
        return repo.replace(regex, function(match, p1, offset, string) {
            return 'https://' + p1.replace(':', '/');
        })
    }
}