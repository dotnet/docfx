// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
var common = require('./common.js');

exports.transform = function (model) {
  var namespaceItems = {
    "class": { inClass: true, typePropertyName: "inClass", id: "classes" },
    "struct": { inStruct: true, typePropertyName: "inStruct", id: "structs" },
    "interface": { inInterface: true, typePropertyName: "inInterface", id: "interfaces" },
    "enum": { inEnum: true, typePropertyName: "inEnum", id: "enums" },
    "delegate": { inDelegate: true, typePropertyName: "inDelegate", id: "delegates" }
  };
  var classItems = {
    "constructor": { inConstructor: true, typePropertyName: "inConstructor", id: "constructors" },
    "field": { inField: true, typePropertyName: "inField", id: "fields" },
    "property": { inProperty: true, typePropertyName: "inProperty", id: "properties" },
    "method": { inMethod: true, typePropertyName: "inMethod", id: "methods" },
    "event": { inEvent: true, typePropertyName: "inEvent", id: "events" },
    "operator": { inOperator: true, typePropertyName: "inOperator", id: "operators" },
    "eii": { inEii: true, typePropertyName: "inEii", id: "eii" }
  };

  if (!model) return null;

  langs = model.langs;
  handleItem(model, model._gitContribute, model._gitUrlPattern);
  if (model.children) {
    model.children.forEach(function (item) { handleItem(item, model._gitContribute, model._gitUrlPattern); });
  }

  if (model.type) {
    switch (model.type.toLowerCase()) {
      case 'namespace':
        model.isNamespace = true;
        if (model.children) groupChildren(model, namespaceItems);
        break;
      case 'class':
      case 'interface':
      case 'struct':
      case 'delegate':
      case 'enum':
        model.isClass = true;
        if (model.children) groupChildren(model, classItems);
        model[namespaceItems[model.type.toLowerCase()].typePropertyName] = true;
        handleNamespace(model);
        break;
      default:
        break;
    }
  }

  return model;
}

exports.getBookmarks = function (model) {
  if (!model || !model.type || model.type.toLowerCase() === "namespace") return null;

  var bookmarks = {};
  // Reference's first level bookmark should have no anchor
  bookmarks[model.uid] = "";

  if (model.children) {
    model.children.forEach(function (item) {
      bookmarks[item.uid] = common.getHtmlId(item.uid);
      if (item.overload && item.overload.uid) {
        bookmarks[item.overload.uid] = common.getHtmlId(item.overload.uid);
      }
    });
  }

  return bookmarks;
}

function groupChildren(model, typeChildrenItems) {
  var grouped = {};

  model.children.forEach(function (c) {
    if (c.isEii) {
      var type = "eii";
    } else {
      var type = c.type.toLowerCase();
    }
    if (!grouped.hasOwnProperty(type)) {
      grouped[type] = [];
    }
    // special handle for field
    if (type === "field" && c.syntax) {
      c.syntax.fieldValue = c.syntax.return;
      c.syntax.return = undefined;
    }
    // special handle for property
    if (type === "property" && c.syntax) {
      c.syntax.propertyValue = c.syntax.return;
      c.syntax.return = undefined;
    }
    // special handle for event
    if (type === "event" && c.syntax) {
      c.syntax.eventType = c.syntax.return;
      c.syntax.return = undefined;
    }
    grouped[type].push(c);
  })
  var children = [];
  for (var key in typeChildrenItems) {
    if (typeChildrenItems.hasOwnProperty(key) && grouped.hasOwnProperty(key)) {
      var typeChildrenItem = typeChildrenItems[key];
      var items = typeChildrenItem.children = grouped[key];
      if (items && items.length > 0) {
        children.push(typeChildrenItem);
      }
    }
  }

  model.children = children;
}

// reserve "namespace" of string for backward compatibility
// will replace "namespace" with "namespaceExpanded" of object
function handleNamespace(model) {
  model.namespaceExpanded = model.namespace;
  if (model.namespaceExpanded) {
    model.namespace = model.namespaceExpanded.uid;
  }
}

function handleItem(vm, gitContribute, gitUrlPattern) {
  // get contribution information
  vm.docurl = common.getImproveTheDocHref(vm, gitContribute, gitUrlPattern);
  vm.sourceurl = common.getViewSourceHref(vm, null, gitUrlPattern);

  // set to null incase mustache looks up
  vm.summary = vm.summary || null;
  vm.remarks = vm.remarks || null;
  vm.conceptual = vm.conceptual || null;
  vm.syntax = vm.syntax || null;
  vm.implements = vm.implements || null;
  common.processSeeAlso(vm);

  // id is used as default template's bookmark
  vm.id = common.getHtmlId(vm.uid);
  if (vm.overload && vm.overload.uid) {
    vm.overload.id = common.getHtmlId(vm.overload.uid);
  }

  // change type in syntax from array to string
  if (vm.syntax) {
    var syntax = vm.syntax;
    if (syntax.parameters) {
      syntax.parameters = syntax.parameters.map(function (p) {
        return joinType(p);
      })
      syntax.parameters = groupParameters(syntax.parameters);
    }
    if (syntax.return) {
      syntax.return = joinType(syntax.return);
    }
  }

  if (vm.supported_platforms) {
    vm.supported_platforms = transformDictionaryToArray(vm.supported_platforms);
  }

  if (vm.requirements) {
    var type = vm.type.toLowerCase();
    if (type == "method") {
      vm.requirements_method = transformDictionaryToArray(vm.requirements);
    } else {
      vm.requirements = transformDictionaryToArray(vm.requirements);
    }
  }

  if (vm && langs) {
    if (shouldHideTitleType(vm)) {
      vm.hideTitleType = true;
    } else {
      vm.hideTitleType = false;
    }

    if (shouldHideSubtitle(vm)) {
      vm.hideSubtitle = true;
    } else {
      vm.hideSubtitle = false;
    }
  }

  function shouldHideTitleType(vm) {
    var type = vm.type.toLowerCase();
    return ((type === 'namespace' && langs.length == 1 && (langs[0] === 'objectivec' || langs[0] === 'java' || langs[0] === 'c'))
      || ((type === 'class' || type === 'enum') && langs.length == 1 && langs[0] === 'c'));
  }

  function shouldHideSubtitle(vm) {
    var type = vm.type.toLowerCase();
    return (type === 'class' || type === 'namespace') && langs.length == 1 && langs[0] === 'c';
  }

  function transformDictionaryToArray(dic) {
    var array = [];
    for (var key in dic) {
      if (dic.hasOwnProperty(key)) {
        array.push({ "name": key, "value": dic[key] })
      }
    }

    return array;
  }

  function joinType(parameter) {
    var joinTypeProperty = function (type, key) {
      if (!type || !type[0] || !type[0][key]) return null;
      var value = type.map(function (t) {
        return t[key][0].value;
      }).join(' | ');
      return [{
        lang: type[0][key][0].lang,
        value: value
      }];
    };
    if (parameter.type) {
      parameter.type = {
        name: joinTypeProperty(parameter.type, "name"),
        nameWithType: joinTypeProperty(parameter.type, "nameWithType"),
        fullName: joinTypeProperty(parameter.type, "fullName"),
        specName: joinTypeProperty(parameter.type, "specName")
      }
    }
    return parameter;
  }

  function groupParameters(parameters) {
    if (!parameters || parameters.length == 0) return parameters;
    var groupedParameters = [];
    var stack = [];
    for (var i = 0; i < parameters.length; i++) {
      var parameter = parameters[i];
      parameter.properties = null;
      var prefixLength = 0;
      while (stack.length > 0) {
        var top = stack.pop();
        var prefix = top.id + '.';
        if (parameter.id.indexOf(prefix) == 0) {
          prefixLength = prefix.length;
          if (!top.parameter.properties) {
            top.parameter.properties = [];
          }
          top.parameter.properties.push(parameter);
          stack.push(top);
          break;
        }
        if (stack.length == 0) {
          groupedParameters.push(top.parameter);
        }
      }
      stack.push({ id: parameter.id, parameter: parameter });
      parameter.id = parameter.id.substring(prefixLength);
    }
    while (stack.length > 0) {
      top = stack.pop();
    }
    groupedParameters.push(top.parameter);
    return groupedParameters;
  }
}