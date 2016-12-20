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

// Note: the parameter `gitContribute` won't be used in this function
function getViewSourceHref(item, gitContribute, gitUrlPattern) {
    if (!item || !item.source || !item.source.remote) return '';
    return getRemoteUrl(item.source.remote, item.source.startLine - '0' + 1, null, gitUrlPattern);
}

function getImproveTheDocHref(item, gitContribute, gitUrlPattern) {
    if (!item) return '';
    if (!item.documentation || !item.documentation.remote) {
        return getNewFileUrl(item, gitContribute, gitUrlPattern);
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
        'generateUrl': function (gitInfo) {
            var url = normalizeGitUrlToHttps(gitInfo.repo);
            url += '/blob' + '/' + gitInfo.branch + '/' + gitInfo.path;
            if (gitInfo.startLine && gitInfo.startLine > 0) {
                url += '/#L' + gitInfo.startLine;
            }
            return url;
        },
        'generateNewFileUrl': function (gitInfo, uid) {
            var url = normalizeGitUrlToHttps(gitInfo.repo);
            url += '/new';
            url += '/' + gitInfo.branch;
            url += '/' + getOverrideFolder(gitInfo.apiSpecFolder);
            url += '/new?filename=' + getHtmlId(uid) + '.md';
            url += '&value=' + encodeURIComponent(getOverrideTemplate(uid));
            return url;
        }
    },
    'vso': {
        'testRegex': /^https:\/\/.*\.visualstudio\.com\/.*/i,
        'generateUrl': function (gitInfo) {
            var url = gitInfo.repo + '?path=' + gitInfo.path + '&version=GB' + gitInfo.branch;
            if (gitInfo.startLine && gitInfo.startLine > 0) {
                url += '&line=' + gitInfo.startLine;
            }
            return url;
        },
        'generateNewFileUrl': function (gitInfo, uid) {
            return '';
        }
    }
}

function normalizeGitUrlToHttps(repo) {
    var pos = repo.indexOf('@');
    if (pos == -1) return repo;
    return 'https://' + repo.substr(pos + 1).replace(/:/g, '/');
}

function getNewFileUrl(item, gitContribute, gitUrlPattern) {
    // do not support VSO for now
    if (!item.source) {
        return '';
    }

    var gitInfo = getGitInfo(gitContribute, item.source.remote);
    if (!gitInfo.repo || !gitInfo.branch || !gitInfo.path) {
        return '';
    }

    if (gitInfo.repo.substr(-4) === '.git') {
        gitInfo.repo = gitInfo.repo.substr(0, gitInfo.repo.length - 4);
    }

    var patternName = getPatternName(gitInfo.repo, gitUrlPattern);
    if (!patternName) return patternName;
    return gitUrlPatternItems[patternName].generateNewFileUrl(gitInfo, item.uid);
}

function getRemoteUrl(remote, startLine, gitContribute, gitUrlPattern) {
    var gitInfo = getGitInfo(gitContribute, remote);
    if (!gitInfo.repo || !gitInfo.branch || !gitInfo.path) {
        return '';
    }

    if (gitInfo.repo.substr(-4) === '.git') {
        gitInfo.repo = gitInfo.repo.substr(0, gitInfo.repo.length - 4);
    }

    var patternName = getPatternName(gitInfo.repo, gitUrlPattern);
    if (!patternName) return '';

    gitInfo.startLine = startLine;
    return gitUrlPatternItems[patternName].generateUrl(gitInfo);
}

function getGitInfo(gitContribute, gitRemote) {
    // apiSpecFolder defines the folder contains overwrite files for MRef, the default value is apiSpec
    var defaultApiSpecFolder = "apiSpec";

    if (!gitContribute && !gitRemote) {
        return {
            apiSpecFolder: defaultApiSpecFolder
        }
    }

    if (!gitContribute) {
        return {
            apiSpecFolder: defaultApiSpecFolder,
            repo: gitRemote.repo,
            branch: gitRemote.branch,
            // path defines the relative path from current git repo.
            path: gitRemote.path
        }
    }

    return {
        apiSpecFolder: gitContribute.apiSpecFolder || defaultApiSpecFolder,
        repo: gitContribute.repo || gitRemote.repo,
        branch: gitContribute.branch || gitRemote.branch,
        path: gitRemote.path
    }
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
