// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
exports.path = {};
exports.path.getFileNameWithoutExtension = getFileNameWithoutExtension;
exports.path.getDirectoryName = getDirectoryName;

exports.getHtmlId = getHtmlId;

exports.getViewSourceHref = getViewSourceHref;
exports.getImproveTheDocHref = getImproveTheDocHref;
exports.processSeeAlso = processSeeAlso;

exports.isAbsolutePath = function (path) {
    return /^(\w+:)?\/\//g.test(path);
}

exports.isRelativePath = function (path) {
    if (!path) return false;
    return !exports.isAbsolutePath(path);
}

function getDirectoryName(path) {
    if (!path) return '';
    var index = path.lastIndexOf('/');
    return path.slice(0, index + 1);
}

function getFileNameWithoutExtension(path) {
    if (!path || path[path.length - 1] === '/' || path[path.length - 1] === '\\') return '';
    var fileName = path.split('\\').pop().split('/').pop();
    return fileName.slice(0, fileName.lastIndexOf('.'));
}

function getHtmlId(input) {
    return input.replace(/\W/g, '_');
}

function getViewSourceHref(item, gitContribute) {
    /* jshint validthis: true */
    if (!item || !item.source || !item.source.remote) return '';
    return getRemoteUrl(item.source.remote, item.source.startLine - '0' + 1, gitContribute);
}

function getImproveTheDocHref(item, gitContribute) {
    if (!item) return '';
    if (!item.documentation || !item.documentation.remote) {
        return getNewFileUrl(item.uid, gitContribute);
    } else {
        return getRemoteUrl(item.documentation.remote, item.documentation.startLine + 1, gitContribute);
    }
}

function getNewFileUrl(uid, gitContribute) {
    // do not support VSO for now
    if (gitContribute && gitContribute.repo) {
        var repo = gitContribute.repo;
        if (repo.substr(-4) === '.git') {
            repo = repo.substr(0, repo.length - 4);
        }
        var path = getGithubUrlPrefix(repo);
        if (path != '') {
            path += '/new';
            path += '/' + gitContribute.branch;
            path += '/' + getOverrideFolder(gitContribute.path);
            path += '/new?filename=' + getHtmlId(uid) + '.md';
            path += '&value=' + encodeURIComponent(getOverrideTemplate(uid));
        }
        return path;
    } else {
        return '';
    }
}

function getRemoteUrl(remote, startLine, gitContribute) {
    if (remote && remote.repo) {
        var repo = remote.repo;
        if (gitContribute && gitContribute.repo) repo = gitContribute.repo;
        var branch = remote.branch;
        if (gitContribute && gitContribute.branch) branch = gitContribute.branch;
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
            path += '/blob' + '/' + branch + '/' + remote.path;
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
    return repo.replace(regex, function (match, p1, offset, string) {
        return 'https://' + p1.replace(':', '/');
    })
}

function getOverrideFolder(path) {
    if (!path) return "";
    path = path.replace('\\', '/');
    if (path.charAt(path.length - 1) == '/') path = path.substring(0, path.length - 1);
    return path;
}

function getOverrideTemplate(uid) {
    if (!uid) return "";
    var content = "";
    content += "---\n";
    content += "uid: " + uid + "\n";
    content += "summary: '*You can override summary for the API here using *MARKDOWN* syntax'\n";
    content += "---\n";
    content += "\n";
    content += "*Please type below more information about this API:*\n";
    content += "\n";
    return content;
}

function processSeeAlso(item) {

    if (item.seealso) {
        for (var key in item.seealso) {
            addIsCref(item.seealso[key]);
        }
    }
    item.seealso = item.seealso || null;
}

function addIsCref(seealso) {
    if (!seealso.linkType || seealso.linkType.toLowerCase() == "cref") {
        seealso.isCref = true;
    }
}
