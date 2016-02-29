// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
function transform(model, _attrs, _global) {
  var util = new Utility();
  var namespaceItems = {
    "class":        { inClass: true,        typePropertyName: "inClass",        id: "classes" },
    "struct":       { inStruct: true,       typePropertyName: "inStruct",       id: "structs" },
    "interface":    { inInterface: true,    typePropertyName: "inInterface",    id: "interfaces" },
    "enum":         { inEnum: true,         typePropertyName: "inEnum",         id: "enums" },
    "delegate":     { inDelegate: true,     typePropertyName: "inDelegate",     id: "delegates" }
  };
  var classItems = {
    "constructor":  { inConstructor: true,  typePropertyName: "inConstructor",  id: "constructors" },
    "field":        { inField: true,        typePropertyName: "inField",        id: "fields" },
    "property":     { inProperty: true,     typePropertyName: "inProperty",     id: "properties" },
    "method":       { inMethod: true,       typePropertyName: "inMethod",       id: "methods" },
    "event":        { inEvent: true,        typePropertyName: "inEvent",        id: "events" },
    "operator":     { inOperator: true,     typePropertyName: "inOperator",     id: "operators" }
  };

  model = createViewModel(model, _attrs, _global);

  model._disableToc = model._disableToc ||!_attrs._tocPath || (_attrs._navPath === _attrs._tocPath);

  return model;

  function createViewModel(model, _attrs, _global) {
    if (!model || !model.items || model.items.length === 0) return null;

    // Pickup the first item and display
    var item = model.items[0];
    var refs = new References(model);
    var mta = model;
    mta.items = undefined;
    mta.references = undefined;
    if (item.type) {
      switch (item.type.toLowerCase()) {
        case 'namespace':
          return new NamespaceViewModel(item, _attrs, _global, refs, mta);
        case 'class':
        case 'interface':
        case 'struct':
        case 'delegate':
        case 'enum':
          return new ClassViewModel(item, _attrs, _global, refs, mta);
        default:
          break;
      }
    }

    return new GeneralViewModel(item, _attrs, _global, refs, mta);

    function GeneralViewModel(item, _attrs, _global, refs, mta) {
      if (_global) {
        this.__global = {};
        for (var key in _global) {
          if (_global.hasOwnProperty(key)) {
            this.__global[key] = _global[key];
          }
        }
      }
      
      for (var key in _attrs) {
        if (_attrs.hasOwnProperty(key)) {
          this[key] = _attrs[key];
        }
      }

      for (var key in mta) {
        if (mta.hasOwnProperty(key)) {
          this[key] = mta[key];
        }
      }

      if (refs) {
        this.item = refs.getViewModel(item.uid, item.langs, util.changeExtension(this._ext), this.newFileRepository);
      }
    }

    function NamespaceViewModel(item, _attrs, _global, refs, mta) {
      GeneralViewModel.call(this, item, _attrs, _global, refs, mta);
      this.isNamespace = true;

      if (this.item.children) {
        var grouped = {};
        // group children with their type
        var that = this;
        this.item.children.forEach(function (c) {
          c = refs.getViewModel(c, item.langs, util.changeExtension(that._ext), that.newFileRepository);
          var type = c.type;
          if (!grouped.hasOwnProperty(type)) {
            grouped[type] = [];
          }
          grouped[type].push(c);
        })
        var children = [];
        for (var key in namespaceItems){
          if (namespaceItems.hasOwnProperty(key) && grouped.hasOwnProperty(key)){
            var namespaceItem = namespaceItems[key];
            var items = namespaceItem.children = grouped[key];
            if (items && items.length > 0) {
              children.push(namespaceItem);
            }
          }
        }

        this.item.children = children;
      }
      this.title = this.item.name[0].value + " " + this.item.type;
    }

    function ClassViewModel(item, _attrs, _global, refs, mta) {
      GeneralViewModel.call(this, item, _attrs, _global, refs, mta);
      this.isClass = true;

      if (this.item.children) {
        var grouped = {};
        var that = this;
        // group children with their type
        this.item.children.forEach(function (c) {
          c = refs.getViewModel(c, item.langs, util.changeExtension(that._ext), that.newFileRepository);
          var type = c.type;
          if (!grouped.hasOwnProperty(type)) {
            grouped[type] = [];
          }
          // special handle for property
          if (type === "Property" && c.syntax) {
            c.syntax.propertyValue = c.syntax.return;
            c.syntax.return = undefined;
          }
          grouped[type].push(c);
        })
        var children = [];
        for (var key in classItems){
          if (classItems.hasOwnProperty(key) && grouped.hasOwnProperty(key)){
            var classItem = classItems[key];
            var items = classItem.children = grouped[key];
            if (items && items.length > 0) {
              children.push(classItem);
            }
          }
        }

        this.item.children = children;
      }
      this[namespaceItems[this.item.type].typePropertyName] = true;
    }

    function References(model) {
      var references = mapping(model.items, mapping(model.references));
      this.getRefvm = getRefvm;
      this.getViewModel = getViewModel;

      function getViewModel(uid, langs, extChanger, newFileRepository) {
        var vm = getRefvm(uid, langs, extChanger);
        vm.docurl = getImproveTheDocHref(vm, newFileRepository);
        vm.sourceurl = getViewSourceHref(vm);

        if (vm.inheritance) {
          vm.inheritance = vm.inheritance.map(function (c, i) {
            var inhe = getRefvm(c, langs, extChanger);
            inhe.index = i;
            return inhe;
          })
        }

        var syntax = vm.syntax;
        if (syntax) {
          if (syntax.parameters) {
            syntax.parameters = syntax.parameters.map(function (currentValue, index, array) {
              currentValue.type = getRefvm(currentValue.type, langs, extChanger);
              return currentValue;
            });
          }
          if (syntax.return) {
            syntax.return.type = getRefvm(syntax.return.type, langs, extChanger);
          }
        }

        if (vm.exceptions) {
          vm.exceptions.forEach(function(i) {
            i.type = getRefvm(i.type, langs, extChanger);
          });
        }

        return vm;
      }

      function getRefvm(uid, langs, extChanger) {
        if (!util.isString(uid)) {
          console.error("should be uid format: " + uid);
          return uid;
        }

        if (references[uid] === undefined) {
          var xref = getXref(uid);
          return {
            specName: langs.map(function(l) {
              return {"lang": l, "value": xref};
            })
          }
        };
        return new Reference(references[uid], this).getReferenceViewModel(langs, extChanger);
      }

      /*
        map array to dictionary with uid as the key
      */
      function mapping(arr, obj) {
        if (!arr) return null;
        if (!obj) obj = {};
        for (var i = arr.length - 1; i >= 0; i--) {
          var item = arr[i];
          if (item.uid) obj[item.uid] = item;
        };
        return obj;
      }

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
    }

    function getXref(uid, fullName, name) {
      var xref = '<xref href="' + util.escapeHtml(uid) + '"';
      if (fullName) xref += ' fullName="' + util.escapeHtml(fullName) + '"';
      if (name) xref += ' name="' + util.escapeHtml(name) + '"';
      xref += '/>';
      return xref;
    }

    function Reference(obj, refs) {
      var _obj = obj;
      this.getReferenceViewModel = getReferenceViewModel;
      this.getTocViewModel = getTocViewModel;

      function getTocViewModel(lang, extChanger) {
        var vm = {};
        // Copy other properties and override name/id
        for (var key in _obj) {
          if (_obj.hasOwnProperty(key)) {
            vm[key] = _obj[key];
          }
        }
        // if homepage is defined, override href with homepage
        if (extChanger) vm.href = extChanger(vm.homepage || vm.href);
        // vm.name = getLinkText(vm.href, vm.name);

        if (vm.items) {
          vm.items = _obj.items.map(function (c) {
            return new Reference(c, refs).getTocViewModel(lang, extChanger);
          })
        }

        return vm;
      }

      function getReferenceViewModel(langs, extChanger) {
        var vm = {};

        // Copy other properties and override name/id
        for (var key in _obj) {
          if (_obj.hasOwnProperty(key)) {
            vm[key] = _obj[key];
          }
        }
        vm.specName = getSpecNameForAllLang(langs, extChanger);
        vm.name = getLangFullCoveredProperty.call(vm, "name", langs) || vm.uid; // workaround bug for dynamic
        vm.fullName = getLangFullCoveredProperty.call(vm, "fullName", langs);
        vm.href = extChanger(vm.href);
        vm.id = getHtmlId(vm.uid);
        vm.summary = vm.summary;
        vm.remarks = vm.remarks;
        vm.conceptual = vm.conceptual;

        vm.level = vm.inheritance ? vm.inheritance.length : 0;
        if (vm.syntax && typeof(vm.syntax.content) != "object") {
          vm.syntax.content = getLangFullCoveredProperty.call(vm.syntax, "content", langs);
        }
        return vm;
      }

      function getHtmlId(input) {
        return input.replace(/\W/g, '_');
      }

      function getSpecNameForAllLang(langs, extChanger) {
        return langs.map(function(l) {
          return {"lang": l, "value": getSpecName(l, extChanger)};
        })
      }

      function getSpecName(lang, extChanger) {
        var name = '';
        var spec = _obj["spec." + lang];
        if (spec && util.isArray(spec)) {
          spec.forEach(function (s) {
            // TODO: sanitize s.name first incase it is < or >
            // TODO: what about href?
            name += getCompositeName.call(s, lang, extChanger);
          });

          return name;
        }

        return getXref(_obj.uid, _obj.fullName, _obj.name);
      }

      function getCompositeName(lang, extChanger) {
        // If href exists, return name with href, elsewise, return full name
        var name = getLangSpecifiedProperty.call(this, "name", lang);
        var fullName = getLangSpecifiedProperty.call(this, "fullName", lang) || name;
        // If href does not exists, return full name
        if (!this.uid) { return util.escapeHtml(fullName); }
        return getXref(this.uid, fullName, name);
      }

      function getLangFullCoveredProperty(key, langs) {
        var that = this;
        return langs.map(function(l) {
          return {"lang": l, "value": getLangSpecifiedProperty.call(that, key, l)};
        })
      }

      function getLangSpecifiedProperty(key, lang) {
        return this[key + "." + lang] || this[key];
      }

      function getLinkText(href, name) {
        return '<a href="' + href + '">' + util.escapeHtml(name) + '</a>';
      }
    }
  }

  function Utility() {
    this.isArray = isArray;
    this.isString = isString;
    this.escapeHtml = escapeHtml;
    this.changeExtension = changeExtension;

    function isArray(arr) {
      if (Object.prototype.toString.call(arr) === '[object Array]') return true;
      return false;
    }

    function isString(input) {
      return typeof input === 'string' || input instanceof String;
    }

    function escapeHtml(str) {
      if (!str) return str;

      var entityMap = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#39;',
        '/': '&#x2F;'
      };
      return str.replace(/[&<>"'\/]/g, function (s) {
        return entityMap[s];
      });
    }

    function changeExtension(e) {
      var ext = e;
      return function (path) {
        // if ext is empty, remove current extension
        // if path ends with '/' or '\', consider it as a folder, extension not added
        if (!path || ext === undefined || path[path.length - 1] === '/' || path[path.length - 1] === '\\') return path;
        var pathWithoutExt = path.slice(0, path.lastIndexOf('.'));
        if (ext && ext[0] !== '.') return pathWithoutExt + '.' + ext;

        return pathWithoutExt + ext;
      }
    }
  }
}
