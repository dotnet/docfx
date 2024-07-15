// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
var extension = require('./toc.extension.js')

exports.transform = function (model) {

  if (extension && extension.preTransform) {
    model = extension.preTransform(model);
  }

  if (model.memberLayout === 'SeparatePages') {
    model = transformMemberPage(model);
  } else {
    transformItem(model, 1);
    if (model.items && model.items.length > 0) model.leaf = false;
    model.title = "Table of Content";
    model._disableToc = true;
  }

  if (extension && extension.postTransform) {
    model = extension.postTransform(model);
  }

  return model;

  function transformItem(item, level) {
    // set to null incase mustache looks up
    item.topicHref = item.topicHref || null;
    item.tocHref = item.tocHref || null;
    item.name = item.name || null;

    item.level = level;
    if (item.items && item.items.length > 0) {
      var length = item.items.length;
      for (var i = 0; i < length; i++) {
        transformItem(item.items[i], level + 1);
      };
    } else {
      item.items = [];
      item.leaf = true;
    }
  }
}

function transformMemberPage(model) {
  var groupNames = {
      "constructor": { key: "constructorsInSubtitle" },
      "field":       { key: "fieldsInSubtitle" },
      "property":    { key: "propertiesInSubtitle" },
      "method":      { key: "methodsInSubtitle" },
      "event":       { key: "eventsInSubtitle" },
      "operator":    { key: "operatorsInSubtitle" },
      "eii":         { key: "eiisInSubtitle" },
      "class":       { key: "classesInSubtitle" },
      "struct":      { key: "structsInSubtitle" },
      "enum":        { key: "enumsInSubtitle" },
      "interface":   { key: "interfacesInSubtitle" },
      "namespace":   { key: "namespacesInSubtitle" },
      "delegate":    { key: "delegatesInSubtitle" },
  };

  groupChildren(model);
  transformItem(model, 1);
  return model;

  function groupChildren(item) {
      if (!item || !item.items || item.items.length == 0) {
          return;
      }
      var grouped = {};
      var items = [];
      item.items.forEach(function (element) {
          groupChildren(element);
          if (element.type) {
              var type = element.isEii ? "eii" : element.type.toLowerCase();
              if (!grouped.hasOwnProperty(type)) {
                  if (!groupNames.hasOwnProperty(type)) {
                      groupNames[type] = {
                          name: element.type
                      };
                      console.log(type + " is not predefined type, use its type name as display name.")
                  }
                  grouped[type] = [];
              }
              grouped[type].push(element);
          } else {
              items.push(element);
          }
      }, this);

      // With order defined in groupNames
      for (var key in groupNames) {
          if (groupNames.hasOwnProperty(key) && grouped.hasOwnProperty(key)) {
              items.push({
                  name: model.__global[groupNames[key].key] || groupNames[key].name,
                  items: grouped[key]
              })
          }
      }

      item.items = items;
  }

  function transformItem(item, level) {
      // set to null in case mustache looks up
      item.topicHref = item.topicHref || null;
      item.tocHref = item.tocHref || null;
      item.name = item.name || null;

      item.level = level;

      if (item.items && item.items.length > 0) {
          item.leaf = false;
          var length = item.items.length;
          for (var i = 0; i < length; i++) {
              transformItem(item.items[i], level + 1);
          };
      } else {
          item.items = [];
          item.leaf = true;
      }
  }
}
