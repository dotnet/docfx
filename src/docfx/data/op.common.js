// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
exports.getCanonicalUrl = function (canonicalUrlPrefix, path, layout, versionPath) {
    if (!canonicalUrlPrefix || !path) return '';
    if (canonicalUrlPrefix[canonicalUrlPrefix.length - 1] == '/') {
      canonicalUrlPrefix = canonicalUrlPrefix.slice(0, -1);
    }

    var pathWithoutVersionPath = path;
    if (versionPath) {
      if (versionPath[versionPath.length - 1] !== '/') {
        versionPath = versionPath + '/';
      }
      if (exports.startsWith(pathWithoutVersionPath, versionPath)) {
        pathWithoutVersionPath = pathWithoutVersionPath.substring(versionPath.length);
      }
    }

    var encodedPath = exports.encodePath(pathWithoutVersionPath);

    var canonicalUrl = canonicalUrlPrefix + "/" + exports.removeExtension(encodedPath);
    canonicalUrl = canonicalUrl.toLowerCase();

    if (typeof (layout) !== "undefined" && exports.endsWith(canonicalUrl, "/index"))
    {
      canonicalUrl = canonicalUrl.slice(0, -5);
    }

    return canonicalUrl;
  }

  exports.endsWith = function (str, suffix) {
      if (str.length < suffix.length)
      {
        return false;
      }
      return str.indexOf(suffix, str.length - suffix.length) !== -1;
  }

  exports.startsWith = function (str, prefix) {
      if (str.length < prefix.length) {
          return false;
      }
      return str.indexOf(prefix) === 0;
  }

  exports.getAssetId = function (item) {
    if (!item || !item.uid) return '';
    return item.uid;
  }

  exports.removeExtension = function (path) {
    var index = path.lastIndexOf('.');
    if (index > 0) {
      return path.substring(0, index);
    }
    return path;
  }

  exports.resetKeysAndSystemAttributes = function (model, resetKeys, keepOpAttributes){
    return exports.batchSetProperties(
      model,
      function (key) {
        if (exports.isArray(resetKeys) && resetKeys.indexOf(key) > -1) {
          return true;
        }
        if (key.indexOf('_op_') === 0 && keepOpAttributes) {
          return false;
        }
        return key.indexOf('_') === 0;
      },
      undefined);
  }

  exports.batchSetProperties = function (model, keySelector, setter) {
    if (model === undefined || keySelector === undefined) return;
    for (var key in model) {
      if (keySelector(key) && model.hasOwnProperty(key)) {
        var element = model[key];
        if (setter === undefined) {
          model[key] = undefined;
        }else{
          model[key] = setter(key);
        }
      }
    }
    return model;
  }

  exports.isArray = function (input){
    return input && Array.isArray(input);
  }

  exports.encodePath = function (path) {
      var splitPaths = path.split(/\/|\\/);
      for (i = 0; i < splitPaths.length; i++) {
          splitPaths[i] = encodeURIComponent(splitPaths[i]);
      }
      return splitPaths.join('/');
  }

  exports.resolveSourcePath = function (model) {
    if (model && model.breadcrumb_path && model._path) {
      model.breadcrumb_path = templateUtility.resolveSourceRelativePath(model.breadcrumb_path, model._path);
    }
  }

  exports.resolvePdfUrlTemplate = function (model) {
      if (model._op_pdfUrlPrefixTemplate) {
          var pdfUrlPrefixTemplate = model._op_pdfUrlPrefixTemplate;
          if (pdfUrlPrefixTemplate[pdfUrlPrefixTemplate.length - 1] === '/') {
              pdfUrlPrefixTemplate = pdfUrlPrefixTemplate.slice(0, -1);
          }
          model.pdf_url_template = pdfUrlPrefixTemplate + '{pdfName}';
      }
  }

  exports.union = function (a, b) {
      if (!b || !Array.isArray(b)) return a;
      if (!a || !Array.isArray(a)) return b;
      return a.concat(b.filter(function (item) {
          return a.indexOf(item) < 0;
      }));
  }

  exports.difference = function (a, b) {
      if (!a || !b || !Array.isArray(a) || !Array.isArray(b)) return a;
      return a.filter(function (item) {
          return b.indexOf(item) < 0;
      })
  }
