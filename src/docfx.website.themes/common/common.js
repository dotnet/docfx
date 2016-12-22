// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
exports.path = {};
exports.path.getFileNameWithoutExtension = getFileNameWithoutExtension;
exports.path.getDirectoryName = getDirectoryName;

exports.getHtmlId = getHtmlId;

exports.getViewSourceHref = getViewSourceHref;
exports.getImproveTheDocHref = getImproveTheDocHref;
exports.processSeeAlso = processSeeAlso;

exports.isAbsolutePath = isAbsolutePath;
exports.isRelativePath = isRelativePath;

function getFileNameWithoutExtension(path) {
    if (!path || path[path.length - 1] === '/' || path[path.length - 1] === '\\') return '';
    var fileName = path.split('\\').pop().split('/').pop();
    return fileName.slice(0, fileName.lastIndexOf('.'));
}

function getDirectoryName(path) {
    if (!path) return '';
    var index = path.lastIndexOf('/');
    return path.slice(0, index + 1);
}

function getHtmlId(input) {
    return input.replace(/\W/g, '_');
}

function getViewSourceHref(item, gitContribute, gitUrlPattern) {
    /* jshint validthis: true */
    if (!item || !item.source || !item.source.remote) return '';
    return getRemoteUrl(item.source.remote, item.source.startLine - '0' + 1, gitContribute, gitUrlPattern);
}

function getImproveTheDocHref(item, gitContribute, gitUrlPattern) {
    if (!item) return '';
    if (!item.documentation || !item.documentation.remote) {
        return getNewFileUrl(item.uid, gitContribute, gitUrlPattern);
    } else {
        return getRemoteUrl(item.documentation.remote, item.documentation.startLine + 1, gitContribute, gitUrlPattern);
    }
}

function processSeeAlso(item) {
    if (item.seealso) {
        for (var key in item.seealso) {
            addIsCref(item.seealso[key]);
        }
    }
    item.seealso = item.seealso || null;
}

function isAbsolutePath(path) {
    return /^(\w+:)?\/\//g.test(path);
}

function isRelativePath(path) {
    if (!path) return false;
    return !exports.isAbsolutePath(path);
}

var gitUrlPatternItems = {
    'github': {
        'testRegex': /^(https?:\/\/)?(\S+\@)?(\S+\.)?github\.com(\/|:).*/i,
        'generateUrl': function(repo, branch, path, startLine) {
            var url = normalizeGitUrlToHttps(repo);
            url += '/blob' + '/' + branch + '/' + path;
            if (startLine && startLine > 0) {
                url += '/#L' + startLine;
            }
            return url;
        },
        'generateNewFileUrl': function(repo, branch, path, uid) {
            var url = normalizeGitUrlToHttps(repo);
            url += '/new';
            url += '/' + branch;
            url += '/' + getOverrideFolder(path);
            url += '/new?filename=' + getHtmlId(uid) + '.md';
            url += '&value=' + encodeURIComponent(getOverrideTemplate(uid));
            return url;
        }
    },
    'vso': {
        'testRegex': /^https:\/\/.*\.visualstudio\.com\/.*/i,
        'generateUrl': function(repo, branch, path, startLine) {
            var url =  repo + '?path=' + path + '&version=GB' + branch;
            if (startLine && startLine > 0) {
                url += '&line=' + startLine;
            }
            return url;
        },
        'generateNewFileUrl': function(repo, branch, path, uid) {
            return '';
        }
    }
}

function normalizeGitUrlToHttps(repo) {
    var pos = repo.indexOf('@');
    if (pos == -1) return repo;
    return 'https://' + repo.substr(pos + 1).replace(/:/g, '/');
}

function getNewFileUrl(uid, gitContribute, gitUrlPattern) {
    // do not support VSO for now
    if (!gitContribute || !gitContribute.repo) return '';
    var repo = gitContribute.repo;
    if (repo.substr(-4) === '.git') {
        repo = repo.substr(0, repo.length - 4);
    }

    var patternName = getPatternName(repo, gitUrlPattern);
    if (!patternName) return patternName;
    return gitUrlPatternItems[patternName].generateNewFileUrl(repo, gitContribute.branch, gitContribute.path, uid);
}

function getRemoteUrl(remote, startLine, gitContribute, gitUrlPattern) {
    var repo = undefined;
    var branch = undefined;
    if (gitContribute && gitContribute.repo) repo = gitContribute.repo;
    if (repo == undefined && remote && remote.repo) repo = remote.repo;
    if (gitContribute && gitContribute.branch) branch = gitContribute.branch;
    if (branch == undefined && remote && remote.branch) branch = remote.branch;

    if (repo == undefined || branch == undefined) return '';

    if (repo.substr(-4) === '.git') {
        repo = repo.substr(0, repo.length - 4);
    }

    var patternName = getPatternName(repo, gitUrlPattern);
    if (!patternName) return '';
    return gitUrlPatternItems[patternName].generateUrl(repo, branch, remote.path, startLine);
}

function getPatternName(repo, gitUrlPattern) {
    if (gitUrlPattern && gitUrlPattern.toLowerCase() in gitUrlPatternItems) {
        return gitUrlPattern.toLowerCase();
    } else {
        for (var p in gitUrlPatternItems) {
            if (gitUrlPatternItems[p].testRegex.test(repo)) {
                return p;
            }
        }
    }
    return '';
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

function addIsCref(seealso) {
    if (!seealso.linkType || seealso.linkType.toLowerCase() == "cref") {
        seealso.isCref = true;
    }
}
