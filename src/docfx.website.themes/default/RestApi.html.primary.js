// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
var common = require('./common.js');

function transform(model, _attrs) {
    var vm = {};
    // Copy default _attrs and override name/id
    for (var key in _attrs) {
        if (_attrs.hasOwnProperty(key)) {
            vm[key] = _attrs[key];
        }
    }
    // Copy model
    for (var key in model) {
        if (model.hasOwnProperty(key)) {
            vm[key] = model[key];
        }
    }
    var _fileNameWithoutExt = common.path.getFileNameWithoutExtension(_attrs._path);
    vm._jsonPath = _fileNameWithoutExt + ".swagger.json";
    vm._disableToc = vm._disableToc || !vm._tocPath || (vm._navPath === vm._tocPath);
    vm.title = vm.title || vm.name;

    vm.docurl = vm.docurl || common.getImproveTheDocHref(vm, vm.newFileRepository);
    vm.sourceurl = vm.sourceurl || common.getViewSourceHref(vm);
    if (vm.children) {
        var ordered = [];
        for (var i = 0; i < vm.children.length; i++) {
            var child = vm.children[i];
            child.docurl = child.docurl || common.getImproveTheDocHref(child, vm.newFileRepository);
            if (child.operation) {
                child.operation = child.operation.toUpperCase();
            }
            child.path = appendQueryParamsToPath(child.path, child.parameters);
            child.sourceurl = child.sourceurl || common.getViewSourceHref(child);
            child.conceptual = child.conceptual || ''; // set to empty incase mustache looks up
            child.footer = child.footer || ''; // set to empty incase mustache looks up
            if (vm.sections && child.uid) {
                var index = vm.sections.indexOf(child.uid);
                if (index > -1) {
                    ordered[index] = child;
                }
            }

            formatExample(child.responses);
            vm.children[i] = transformReference(vm.children[i]);
        };
        if (vm.sections) {
            // Remove empty values from ordered, in case item in sections is not in swagger json 
            vm.children = ordered.filter(function(o) { return o; });
        }
    }

    return vm;

    function formatExample(responses) {
        if (!responses) return;
        for (var i = responses.length - 1; i >= 0; i--) {
            var examples = responses[i].examples;
            if (!examples) continue;
            for (var j = examples.length - 1; j >= 0; j--) {
                var content = examples[j].content;
                if (!content) continue;
                var mimeType = examples[j].mimeType;
                if (mimeType === 'application/json') {
                    try {
                        var json = JSON.parse(content)
                        responses[i].examples[j].content = JSON.stringify(json, null, '  ');
                    } catch (e) {
                        console.warn("example is not a valid JSON object.");
                    }
                }
            }
        }
    }

    function transformReference(obj) {
        if (Array.isArray(obj)) {
            for (var i = 0; i < obj.length; i++) {
                obj[i] = transformReference(obj[i]);
            }
        }
        else if (typeof obj === "object") {
            for (var key in obj) {
                if (obj.hasOwnProperty(key)) {
                    if (key === "schema") {
                        // transform schema.properties from obj to key value pair
                        obj[key] = transformProperties(obj[key]);
                    }
                    else {
                        obj[key] = transformReference(obj[key]);
                    }
                }
            }
        }
        return obj;
    }

    function transformProperties(obj) {
        if (obj.properties) {
            if (obj.required && Array.isArray(obj.required)) {
                for (var i = 0; i < obj.required.length; i++) {
                    var field = obj.required[i];
                    if (obj.properties[field]) {
                        // add required field as property
                        obj.properties[field].required = true;
                    }
                }
                delete obj.required;
            }
            var array = [];
            for (var key in obj.properties) {
                if (obj.properties.hasOwnProperty(key)) {
                    var value = obj.properties[key];
                    // set description to null incase mustache looks up
                    value.description = value.description || null;

                    value = transformPropertiesValue(value);
                    array.push({ key: key, value: value });
                }
            }
            obj.properties = array;
        }
        return obj;
    }

    function transformPropertiesValue(obj) {
        if (obj.type === "array" && obj.items) {
            obj.items.properties = obj.items.properties || null;
            obj.items = transformProperties(obj.items);
        }
        return obj;
    }

    function appendQueryParamsToPath(path, parameters) {
        if (!path || !parameters) return path;

        var requiredQueryParams = parameters.filter(function(p) { return p.in === 'query' && p.required; });
        if (requiredQueryParams.length > 0) {
            path = formatParams(path, requiredQueryParams, true);
        }

        var optionalQueryParams = parameters.filter(function(p) { return p.in === 'query' && !p.required; });
        if (optionalQueryParams.length > 0) {
            path += "[";
            path = formatParams(path, optionalQueryParams, requiredQueryParams.length == 0);
            path += "]";
        }
        return path;
    }

    function formatParams(path, parameters, isFirst) {
        for (var i = 0; i < parameters.length; i++) {
            if (i == 0 && isFirst) {
                path += "?";
            } else {
                path += "&";
            }
            path += parameters[i].name;
        }
        return path;
    }
}
