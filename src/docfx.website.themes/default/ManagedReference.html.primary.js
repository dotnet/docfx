// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
// TODO: support multiple languages: [].concat(langs)
function transform(model, _attrs) {
  var util = new Utility();
  var namespaceItems = {
    "class":        { name: "Class",      title: "Classes",     id: "classes" },
    "struct":       { name: "Struct",     title: "Structs",     id: "structs" },
    "interface":    { name: "Interface",  title: "Interfaces",  id: "interfaces" },
    "enum":         { name: "Enum",       title: "Enums",       id: "enums" },
    "delegate":     { name: "Delegate",   title: "Delegates",   id: "delegates" }
  };
  var classItems = {
    "constructor":  { title: "Constructors",  id: "constructors" },
    "field":        { title: "Fields",        id: "fields" },
    "property":     { title: "Properties",    id: "properties" },
    "method":       { title: "Methods",       id: "methods" },
    "event":        { title: "Events",        id: "events" },
    "operator":     { title: "Operators",     id: "operators" }
  };
  if (util.isString(model)) model = JSON.parse(model);

  // attrs contains additional system infomation:
  if (_attrs && util.isString(_attrs)) _attrs = JSON.parse(_attrs);

  model = createViewModel(model, _attrs);
  if (_attrs._navPath === _attrs._tocPath) {
    model._allowToc = false;
  } else {
    model._allowToc = true;
  }

  if (!model.hasOwnProperty("_allowAffix")) {
    model._allowAffix = true;
  } else {
    // parse from string to bool
    model._allowAffix = model._allowAffix === "true"
  }
  return model;

  function createViewModel(model, _attrs) {
    if (!model || !model.items || model.items.length === 0) return null;

    // Pickup the first item and display
    var item = model.items[0];
    var refs = new References(model);
    var mta = model.metadata;
    if (item.type) {
      switch (item.type.toLowerCase()) {
        case 'namespace':
          return new NamespaceViewModel(item, _attrs, refs, mta);
        case 'class':
        case 'interface':
        case 'struct':
        case 'delegate':
        case 'enum':
          return new ClassViewModel(item, _attrs, refs, mta);
        default:
          break;
      }
    }

    return new GeneralViewModel(item, _attrs, refs, mta);

    function GeneralViewModel(item, _attrs, refs, mta) {
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
        this.item = refs.getViewModel(item.uid, this._lang, util.changeExtension(this._ext));
      }
    }

    function NamespaceViewModel(item, _attrs, refs, mta) {
      GeneralViewModel.call(this, item, _attrs, refs, mta);
      this.isNamespace = true;

      if (this.item.children) {
        var grouped = {};
        // group children with their type
        var that = this;
        this.item.children.forEach(function (c) {
          c = refs.getViewModel(c, that._lang, util.changeExtension(that._ext));
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
      this.item.type = "Namespace";
      this.title = this.item.type + " " + this.item.name;
    }

    function ClassViewModel(item, _attrs, refs, mta) {
      GeneralViewModel.call(this, item, _attrs, refs, mta);
      this.isClass = true;

      if (this.item.children) {
        var grouped = {};
        var that = this;
        // group children with their type
        this.item.children.forEach(function (c) {

          c = refs.getViewModel(c, that._lang, util.changeExtension(that._ext));
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
      this.item.type = namespaceItems[this.item.type].name;
      this.title = this.item.type + " " + this.item.name;
    }

    function References(model) {
      var references = mapping(model.items, mapping(model.references));
      this.getRefvm = getRefvm;
      this.getViewModel = getViewModel;

      function getViewModel(uid, lang, extChanger) {
        var vm = getRefvm(uid, lang, extChanger);
        vm.docurl = getImproveTheDocHref(vm);
        vm.sourceurl = getViewSourceHref(vm);

        if (vm.inheritance) {
          vm.inheritance = vm.inheritance.map(function (c, i) {
            var inhe = getRefvm(c, lang, extChanger);
            inhe.index = i;
            return inhe;
          })
        }

        var syntax = vm.syntax;
        if (syntax) {
          if (syntax.parameters) {
            syntax.parameters = syntax.parameters.map(function (currentValue, index, array) {
              currentValue.type = getRefvm(currentValue.type, lang, extChanger);
              return currentValue;
            });
          }
          if (syntax.return) {
            syntax.return.type = getRefvm(syntax.return.type, lang, extChanger);
          }
        }

        if (vm.exceptions) {
          vm.exceptions.forEach(function(i) {
            i.type = getRefvm(i.type, lang, extChanger);
          });
        }

        return vm;
      }

      function getRefvm(uid, lang, extChanger) {
        if (!util.isString(uid)) {
          console.error("should be uid format: " + uid);
          return uid;
        }

        if (references[uid] === undefined) {
          return {
            specName: getXref(uid)
          }
        };
        return new Reference(references[uid], this).getReferenceViewModel(lang, extChanger);
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

      /*
        TODO: integrate with zhyan's change
      */
      function getImproveTheDocHref(item) {
        if (!item || !item.documentation || !item.documentation.remote) return '';
        return getRemoteUrl(item.documentation.remote, item.documentation.startLine + 1);
      }

      function getViewSourceHref(item) {
        /* jshint validthis: true */
        if (!item || !item.source || !item.source.remote) return '';
        return getRemoteUrl(item.source.remote, item.source.startLine - '0' + 1);
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

    function getXref(uid, fullName, name) {
      var xref = '<xref href="' + util.escapeHtml(uid) + '"';
      if (fullName) xref += ' fullName="' + util.escapeHtml(fullName) + '"';
      if (name) xref += '" name="' + util.escapeHtml(name) + '"';
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

      function getReferenceViewModel(lang, extChanger) {
        var name = getLangSpecifiedProperty.call(_obj, "fullName", lang) || getLangSpecifiedProperty.call(_obj, "name", lang);
        var vm = {};

        // Copy other properties and override name/id
        for (var key in _obj) {
          if (_obj.hasOwnProperty(key)) {
            vm[key] = _obj[key];
          }
        }
        vm.specName = getSpecName(lang, extChanger);
        vm.name = getLangSpecifiedProperty.call(vm, "name", lang) || vm.uid; // workaround bug for dynamic
        vm.fullName = getLangSpecifiedProperty.call(vm, "fullName", lang);
        vm.href = extChanger(vm.href);
        vm.id = getHtmlId(vm.uid);
        vm.summary = vm.summary;
        vm.remarks = vm.remarks;
        vm.conceptual = vm.conceptual;

        vm.level = vm.inheritance ? vm.inheritance.length : 0;
        if (vm.syntax) {
          vm.syntax.content = getLangSpecifiedProperty.call(vm.syntax, "content", lang);
        }
        return vm;
      }

      function getHtmlId(input) {
        return input.replace(/\W/g, '_');
      }

      function getSpecName(lang, extChanger) {
        // spec is always language specific
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
        var href = this.href;
        var name = getLangSpecifiedProperty.call(this, "name", lang);
        var fullName = getLangSpecifiedProperty.call(this, "fullName", lang) || name;
        // If href does not exists, return full name
        if (!this.uid) { return util.escapeHtml(fullName); }
        return getXref(this.uid, fullName, name);
      }

      function getName(lang) {
        var fullName = getLangSpecifiedProperty.call(this, "fullName", lang) || getLangSpecifiedProperty.call(this, "name", lang);
        return fullName;
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
        var pathWithoutExt = path.substring(0, path.lastIndexOf('.'));
        if (ext && ext[0] !== '.') return pathWithoutExt + '.' + ext;

        return pathWithoutExt + ext;
      }
    }
  }
}
