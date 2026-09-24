// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
var common = require('./common.js');

exports.transform = function (model) {
    var definitions = Object.create(null);
    var references = [];
    Object.keys(model.schemas || {}).forEach(function (name) { schemaDetails(model.schemas[name], name); });
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
            child.path = child.displayPath || child.path;
            child.sourceurl = child.sourceurl || common.getViewSourceHref(child, null, model._gitUrlPattern);
            child.conceptual = child.conceptual || ''; // set to empty incase mustache looks up
            child.summary = child.summary || ''; // set to empty incase mustache looks up
            child.description = child.description || ''; // set to empty incase mustache looks up
            child.footer = child.footer || ''; // set to empty incase mustache looks up
            child.remarks = child.remarks || ''; // set to empty incase mustache looks up
            child.htmlId = common.getHtmlId(child.uid);

            formatExample(child.responses);
            (child.servers || []).forEach(function (server) { server.description = server.description || ''; });
            (child.parameters || []).forEach(transformPayload);
            if (child.requestBody) {
                child.requestBody.description = child.requestBody.description || '';
                transformContent(child.requestBody.content);
            }
            (child.responses || []).forEach(transformPayload);
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
    references.forEach(function (reference) {
        reference.details.referenceId = definitions[reference.name] ? definitions[reference.name].id : '';
    });
    model.definitions = Object.keys(definitions).map(function (name) {
        var entry = definitions[name];
        var details = Object.assign({}, entry.details, { id: entry.id, name: name });
        if (details.referenceName === name) {
            details.referenceName = '';
            details.referenceId = '';
        }
        return { schemaDetails: details };
    });

    return model;

    function schemaId(name) {
        return "schema-" + name.replace(/[^a-zA-Z0-9-]/g, function (character) {
            return "_" + character.charCodeAt(0).toString(16) + "_";
        });
    }

    function transformPayload(payload) {
        payload.hasContent = payload.content !== undefined && payload.content !== null;
        transformContent(payload.content);
        payload.schemaDetails = schemaDetails(payload.schema);
        payload.exampleDetails = exampleDetails(payload.examples);
    }

    function schemaDetails(schema, definitionName) {
        if (!schema) return false;
        var name = schema['x-internal-loop-ref-name'] || schema['x-internal-ref-name'];
        // Null fields fall through to ancestor scopes in Docfx's Mustache renderer.
        // Empty strings and false keep missing fields local to this schema.
        var details = {};
        [definitionName, schema['x-internal-ref-name']].forEach(function (registeredName) {
            if (registeredName && (registeredName === definitionName || !definitions[registeredName])) {
                definitions[registeredName] = { id: schema.referenceId || schemaId(registeredName), details: details };
            }
        });
        if (name) references.push({ details: details, name: name });
        return Object.assign(details, {
            type: schema.type || '',
            format: schema.format || '',
            description: schema.description || '',
            referenceName: name || '',
            referenceId: '',
            properties: Object.keys(schema.properties || {}).map(function (key) {
                return {
                    key: key,
                    required: schema.properties[key].required === true ||
                        (Array.isArray(schema.required) && schema.required.indexOf(key) >= 0),
                    value: schemaDetails(schema.properties[key])
                };
            }),
            items: schemaDetails(schema.items),
            composition: (schema.allOf ? [{ kind: 'All of', schemas: schema.allOf }] : []).concat(schema.composition || []).map(function (composition) {
                return { kind: composition.kind, schemas: (composition.schemas || []).map(function (branch) { return schemaDetails(branch); }) };
            }),
            constraints: schema.constraints || [],
            enum: (schema.enum || []).map(function (value) { return { value: JSON.stringify(value) }; }),
            exampleDetails: exampleDetails(schema.examples || (schema.example !== undefined ? [{ content: JSON.stringify(schema.example) }] : []))
        });
    }

    function exampleDetails(examples) {
        return (examples || []).map(function (example) {
            var externalValue = example.externalValue || '';
            return {
                name: example.name || '',
                mimeType: example.mimeType || '',
                content: typeof example.content === "string" ? example.content : '',
                hasContent: typeof example.content === "string",
                externalValue: externalValue,
                externalHref: externalValue && /^https?:\/\/[^\s\\]+$/i.test(externalValue) ? externalValue : ''
            };
        });
    }

    function transformContent(content) {
        (content || []).forEach(function (media) {
            media.schemaDetails = schemaDetails(media.schema);
            media.itemSchemaDetails = schemaDetails(media.itemSchema);
            media.examples = media.examples || [];
            media.examples.forEach(function (example) {
                example.name = example.name || '';
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
