// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
var common = require('./common.js');

exports.transform = function (model) {
    var schemas = Object.create(null);
    Object.keys(model.schemas || {}).forEach(function (name) {
        schemas[name] = model.schemas[name];
    });
    Object.keys(schemas).forEach(function (name) { collectSchemas(schemas[name]); });
    (model.children || []).forEach(function (child) {
        if (!(model._preserveLiteralData || child._preserveLiteralData ||
            model.schemas || child.servers || child.requestUrl || child.requestBody ||
            (child.parameters || []).some(function (parameter) { return parameter.content; }) ||
            (child.responses || []).some(function (response) { return response.content; }))) return;
        child._hasSchemaDetails = true;
        (child.parameters || []).forEach(function (parameter) {
            collectSchemas(parameter.schema);
            (parameter.content || []).forEach(function (media) { collectSchemas(media.schema); });
        });
        (child.responses || []).forEach(function (response) {
            collectSchemas(response.schema);
            (response.content || []).forEach(function (media) { collectSchemas(media.schema); });
        });
        ((child.requestBody || {}).content || []).forEach(function (media) { collectSchemas(media.schema); });
    });
    var _fileNameWithoutExt = common.path.getFileNameWithoutExtension(model._path);
    model._jsonPath = _fileNameWithoutExt + ".swagger" + (model.rawExtension === ".yaml" ? ".yaml" : ".json");
    model.title = model.title || model.name;
    model.docurl = model.docurl || common.getImproveTheDocHref(model, model._gitContribute, model._gitUrlPattern);
    model.sourceurl = model.sourceurl || common.getViewSourceHref(model, null, model._gitUrlPattern);
    model.htmlId = common.getHtmlId(model.uid);
    if (model.children) {
        for (var i = 0; i < model.children.length; i++) {
            var child = model.children[i];
            child.docurl = child.docurl || common.getImproveTheDocHref(child, model._gitContribute, model._gitUrlPattern);
            if (child.operation) {
                child.operation = child.operation.toUpperCase();
            }
            if (!child._hasSchemaDetails) {
                child.path = appendQueryParamsToPath(child.path, child.parameters);
            }
            child.sourceurl = child.sourceurl || common.getViewSourceHref(child, null, model._gitUrlPattern);
            child.conceptual = child.conceptual || ''; // set to empty incase mustache looks up
            child.summary = child.summary || ''; // set to empty incase mustache looks up
            child.description = child.description || ''; // set to empty incase mustache looks up
            child.footer = child.footer || ''; // set to empty incase mustache looks up
            child.remarks = child.remarks || ''; // set to empty incase mustache looks up
            child.htmlId = common.getHtmlId(child.uid);

            formatExample(child.responses);
            if (child._hasSchemaDetails) {
                (child.servers || []).forEach(function (server) { server.description = server.description || null; });
                (child.parameters || []).forEach(function (parameter) {
                    parameter.hasContent = parameter.content !== undefined && parameter.content !== null;
                    transformContent(parameter.content);
                    parameter.schemaDetails = schemaDetails(parameter.schema);
                });
                if (child.requestBody) {
                    child.requestBody.description = child.requestBody.description || null;
                    transformContent(child.requestBody.content);
                }
                (child.responses || []).forEach(function (response) {
                    response.hasContent = response.content !== undefined && response.content !== null;
                    transformContent(response.content);
                    response.schemaDetails = schemaDetails(response.schema);
                });
            } else {
                resolveAllOf(child);
                transformReference(child);
            }
        };
        if (!model.tags || model.tags.length === 0) {
            var childTags = [];
            for (var i = 0; i < model.children.length; i++) {
                var child = model.children[i];
                if (child.tags && child.tags.length > 0) {
                    for (var k = 0; k < child.tags.length; k++) {
                        // for each tag in child, add unique tag string into childTags
                        if (childTags.indexOf(child.tags[k]) === -1) {
                            childTags.push(child.tags[k]);
                        }
                    }
                }
            }
            // sort alphabetically
            childTags.sort();
            if (childTags.length > 0) {
                model.tags = [];
                for (var i = 0; i < childTags.length; i++) {
                    // add tags into model
                    model.tags.push({ "name": childTags[i] });
                }
            }
        }
        if (model.tags) {
            for (var i = 0; i < model.tags.length; i++) {
                var children = getChildrenByTag(model.children, model.tags[i].name);
                if (children) {
                    // set children into tag section
                    model.tags[i].children = children;
                }
                model.tags[i].conceptual = model.tags[i].conceptual || ''; // set to empty incase mustache looks up
                if (model.tags[i]["x-bookmark-id"]) {
                    model.tags[i].htmlId = model.tags[i]["x-bookmark-id"];
                } else if (model.tags[i].uid) {
                    model.tags[i].htmlId = common.getHtmlId(model.tags[i].uid);
                }
            }
            for (var i = 0; i < model.children.length; i++) {
                var child = model.children[i];
                if (child.includedInTags) {
                    // set child to undefined, which is already moved to tag section
                    model.children[i] = undefined;
                    if (!model.isTagLayout) {
                        // flags to indicate the model is tag layout
                        model.isTagLayout = true;
                    }
                }
            }
            // remove undefined child
            model.children = model.children.filter(function (o) { return o; });
        }
    }
    model.definitions = [];
    if (model.tags) {
        model.tags.forEach(function(tag) {
            (tag.children || []).forEach(function(child) {
                if (child._hasSchemaDetails) return;
                (child.parameters || []).forEach(function(parameter) { addComplexTypeMetadata(parameter.schema, model.definitions); });
                (child.responses || []).forEach(function(response) { addComplexTypeMetadata(response.schema, model.definitions); });
            });
        });
    }
    if (model.children) {
        model.children.forEach(function(child) {
            if (child._hasSchemaDetails) return;
            (child.parameters || []).forEach(function(parameter) { addComplexTypeMetadata(parameter.schema, model.definitions); });
            (child.responses || []).forEach(function(response) { addComplexTypeMetadata(response.schema, model.definitions); });
        });
    }
    Object.keys(schemas).forEach(function (name) {
        var details = schemaDetails(schemas[name]);
        details.id = schemaId(name);
        details.name = name;
        if (details.referenceName === name) {
            details.referenceName = null;
            details.referenceId = null;
        }
        model.definitions.push({ schemaDetails: details });
    });

    return model;

    function schemaId(name) {
        return "schema-" + name.replace(/[^a-zA-Z0-9-]/g, function (character) {
            return "_" + character.charCodeAt(0).toString(16) + "_";
        });
    }

    function collectSchemas(schema) {
        if (!schema) return;
        var name = schema['x-internal-ref-name'];
        if (name && !schemas[name]) schemas[name] = schema;
        Object.keys(schema.properties || {}).forEach(function (key) { collectSchemas(schema.properties[key]); });
        collectSchemas(schema.items);
        (schema.composition || []).forEach(function (composition) {
            (composition.schemas || []).forEach(collectSchemas);
        });
    }

    function schemaDetails(schema) {
        if (!schema) return null;
        var name = schema['x-internal-loop-ref-name'] || schema['x-internal-ref-name'];
        // Explicit empty fields prevent recursive Mustache partials from looking up an ancestor's schema.
        return {
            type: schema.type || null,
            format: schema.format || null,
            description: schema.description || null,
            referenceName: name || null,
            referenceId: name && schemas[name] ? schemaId(name) : null,
            properties: Object.keys(schema.properties || {}).map(function (key) {
                return {
                    key: key,
                    required: schema.properties[key].required === true ||
                        (Array.isArray(schema.required) && schema.required.indexOf(key) >= 0),
                    value: schemaDetails(schema.properties[key])
                };
            }),
            items: schemaDetails(schema.items),
            composition: (schema.composition || []).map(function (composition) {
                return { kind: composition.kind, schemas: (composition.schemas || []).map(schemaDetails) };
            }),
            constraints: schema.constraints || [],
            enum: (schema.enum || []).map(function (value) { return { value: JSON.stringify(value) }; }),
            exampleDetails: exampleDetails(schema.examples)
        };
    }

    function exampleDetails(examples) {
        return (examples || []).map(function (example) {
            var externalValue = example.externalValue || null;
            return {
                name: example.name || null,
                mimeType: example.mimeType || null,
                content: typeof example.content === "string" ? example.content : null,
                hasContent: typeof example.content === "string",
                externalValue: externalValue,
                externalHref: externalValue && /^https?:\/\/[^\s\\]+$/i.test(externalValue) ? externalValue : null
            };
        });
    }

    function transformContent(content) {
        (content || []).forEach(function (media) {
            media.schemaDetails = schemaDetails(media.schema);
            media.examples = media.examples || [];
            media.examples.forEach(function (example) {
                example.name = example.name || null;
                example.mimeType = example.mimeType || media.mimeType;
            });
        });
        formatExample(content);
        (content || []).forEach(function (media) { media.exampleDetails = exampleDetails(media.examples); });
    }

    function getChildrenByTag(children, tag) {
        if (!children) return;
        return children.filter(function (child) {
            if (child.tags && child.tags.indexOf(tag) > -1) {
                child.includedInTags = true;
                return true;
            }
        })
    }

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

    function resolveAllOf(obj) {
        if (Array.isArray(obj)) {
            for (var i = 0; i < obj.length; i++) {
                resolveAllOf(obj[i]);
            }
        }
        else if (typeof obj === "object") {
            for (var key in obj) {
                if (obj.hasOwnProperty(key)) {
                    if (key === "allOf" && Array.isArray(obj[key])) {
                        // find 'allOf' array and process
                        processAllOfArray(obj[key], obj);
                        // delete 'allOf' value
                        delete obj[key];
                    } else {
                        resolveAllOf(obj[key]);
                    }
                }
            }
        }
    }

    function processAllOfArray(allOfArray, originalObj) {
        // for each object in 'allOf' array, merge the values to those in the same level with 'allOf'
        for (var i = 0; i < allOfArray.length; i++) {
            var item = allOfArray[i];
            for (var key in item) {
                if (originalObj.hasOwnProperty(key)) {
                    mergeObjByKey(originalObj[key], item[key]);
                } else {
                    originalObj[key] = item[key];
                }
            }
        }
    }

    function mergeObjByKey(targetObj, sourceObj) {
        for (var key in sourceObj) {
            // merge only when target object doesn't define the key
            if (!targetObj.hasOwnProperty(key)) {
                targetObj[key] = sourceObj[key];
            }
        }
    }

    function transformReference(obj) {
        if (Array.isArray(obj)) {
            for (var i = 0; i < obj.length; i++) {
                transformReference(obj[i]);
            }
        }
        else if (typeof obj === "object") {
            for (var key in obj) {
                if (obj.hasOwnProperty(key)) {
                    if (key === "schema") {
                        // transform schema.properties from obj to key value pair
                        transformProperties(obj[key]);
                    } else {
                        transformReference(obj[key]);
                    }
                }
            }
        }
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

                    transformPropertiesValue(value);
                    array.push({ key: key, value: value });
                }
            }
            obj.properties = array;
        }
    }

    function transformPropertiesValue(obj) {
        if (obj.type === "array" && obj.items) {
            // expand array to transformProperties
            obj.items.properties = obj.items.properties || null;
            obj.items['x-internal-ref-name'] = obj.items['x-internal-ref-name'] || null;
            obj.items['x-internal-loop-ref-name'] = obj.items['x-internal-loop-ref-name'] || null;
            transformProperties(obj.items);
        } else if (obj.properties && !obj.items) {
            // fill obj.properties into obj.items.properties, to be rendered in the same way with array
            obj.items = {};
            obj.items.properties = obj.properties || null;
            delete obj.properties;
            if (obj.required) {
                obj.items.required = obj.required;
                delete obj.required;
            }
            obj.items['x-internal-ref-name'] = obj['x-internal-ref-name'] || null;
            obj.items['x-internal-loop-ref-name'] = obj['x-internal-loop-ref-name'] || null;
            transformProperties(obj.items);
        }
    }

    function appendQueryParamsToPath(path, parameters) {
        if (!path || !parameters) return path;

        var requiredQueryParams = parameters.filter(function (p) { return p.in === 'query' && p.required; });
        if (requiredQueryParams.length > 0) {
            path = formatParams(path, requiredQueryParams, true);
        }

        var optionalQueryParams = parameters.filter(function (p) { return p.in === 'query' && !p.required; });
        if (optionalQueryParams.length > 0) {
            path += "[";
            path = formatParams(path, optionalQueryParams, requiredQueryParams.length === 0);
            path += "]";
        }
        return path;
    }

    function formatParams(path, parameters, isFirst) {
        for (var i = 0; i < parameters.length; i++) {
            if (i === 0 && isFirst) {
                path += "?";
            } else {
                path += "&";
            }
            path += parameters[i].name;
        }
        return path;
    }

    function addDefinition(definition, definitions) {

        if (!definition) {
            return;
        }

        var xRefName = definition.items && definition.items['x-internal-ref-name']
            ? definition.items['x-internal-ref-name']
            : definition['x-internal-ref-name'];

        // Not complex type.
        if (!xRefName) {
            return;
        }

        // Definition already exists return.
        if (definitions.some(function(d) { return d['x-internal-ref-name'] == xRefName; })) {
            return;
        }

        // Create clone to not affect object structure used in original location
        definition = JSON.parse(JSON.stringify(definition));

        // Unify different object structure to be the same

        // Sometimes properties is under items sometimes not
        if (definition.items && definition.items.properties) {
            definition.properties = definition.items.properties;
        }

        // Sometimes ref-name is under items sometimes not
        definition['x-internal-ref-name'] = xRefName;

        // Sometimes properties are key/value pairs sometimes not
        if (definition.properties && !Array.isArray(definition.properties)) {
            definition.properties = Object.keys(definition.properties).map(function(key) {
                return {
                    key: key,
                    value: definition.properties[key]
                }
            });
        }

        // Add definition to definitions list.
        definitions.push(definition);

        // Loop through properties that refer to other definitions.
        (definition.properties || []).forEach(function(property) {
            addComplexTypeMetadata(property.value, definitions);
        });
    }

    function addComplexTypeMetadata(child, definitions) {
        // Add variations of x-internal-ref-name to support
        if (child && child['x-internal-ref-name']) {
            child.cTypeId = child['x-internal-ref-name'].replace(/\./g, '_');
            child.cType = child['x-internal-ref-name'].replace(/([A-Z])/g, '<wbr>$1');
        }
        if (child && child.items && child.items['x-internal-ref-name']) {
            child.cTypeId = child.items['x-internal-ref-name'].replace(/\./g, '_');
            child.cType = child.items['x-internal-ref-name'].replace(/([A-Z])/g, '<wbr>$1');
            child.cTypeIsArray = true;
        }
        addDefinition(child, definitions);
    }
}

exports.getBookmarks = function (model) {
    if (!model) return null;

    var bookmarks = {};

    bookmarks[model.uid] = "";
    if (model.tags) {
        model.tags.forEach(function (tag) {
            if (tag.uid) {
                bookmarks[tag.uid] = tag["x-bookmark-id"] ? tag["x-bookmark-id"] : common.getHtmlId(tag.uid);
            }
            if (tag.children) {
                tag.children.forEach(function (child) {
                    if (child.uid) {
                        bookmarks[child.uid] = common.getHtmlId(child.uid);
                    }
                })
            }
        })
    }
    if (model.children) {
        model.children.forEach(function (child) {
            if (child.uid) {
                bookmarks[child.uid] = common.getHtmlId(child.uid);
            }
        });
    }

    return bookmarks;
}
