// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Meta data used by the AngularJS docs app
angular.module('pagesData', [])
  .value('NG_PAGES', {});
// Order matters
angular.module('itemTypes', [])
  .value('NG_ITEMTYPES', {
    "class": {
      "Constructor": {
        "id": "ctor",
        "name": "Constructor",
        "description": "Constructors",
        "show": false
      },
      "Field": {
        "id": "field",
        "name": "Field",
        "description": "Fields",
        "show": false
      },
      "Property": {
        "id": "property",
        "name": "Property",
        "description": "Properties",
        "show": false
      },
      "Method": {
        "id": "method",
        "name": "Method",
        "description": "Methods",
        "show": false
      },
      "Operator": {
        "id": "operator",
        "name": "Operator",
        "description": "Operators",
        "show": false
      },
      "Event": {
        "id": "event",
        "name": "Event",
        "description": "Events",
        "show": false
      }
    },
    // [
    //   { "name": "Property", "description": "Property" },
    //   { "name": "Method" , "description": "Method"},
    //   { "name": "Constructor" , "description": "Constructor"},
    //   { "name": "Field" , "description": "Field"},
    // ],
    "namespace": {
      "Class": {
        "id": "class",
        "name": "Class",
        "description": "Classes",
        "show": false
      },
      "Enum": {
        "id": "enum",
        "name": "Enum",
        "description": "Enums",
        "show": false
      },
      "Delegate": {
        "id": "delegate",
        "name": "Delegate",
        "description": "Delegates",
        "show": false
      },
      "Interface": {
        "id": "interface",
        "name": "Interface",
        "description": "Interfaces",
        "show": false
      },
      "Struct": {
        "id": "struct",
        "name": "Struct",
        "description": "Structs",
        "show": false
      },
    },
    // [

    //   { "name": "Class", "description": "Class" },
    //   { "name": "Enum" , "description": "Enum"},
    //   { "name": "Delegate" , "description": "Delegate"},
    //   { "name": "Struct" , "description": "Struct"},
    //   { "name": "Interface", "description": "Interface" },
    // ]
  });