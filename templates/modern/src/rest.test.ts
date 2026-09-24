// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import test from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { runInThisContext } from 'node:vm'

// Docfx loads these CommonJS scripts separately from the template's ES modules.
const common = {}
runInThisContext(`(function(exports) {
  ${readFileSync(new URL('../../common/common.js', import.meta.url), 'utf8')}
})`)(common)
const rest = runInThisContext(`(function(require) {
  const exports = {};
  ${readFileSync(new URL('../../common/RestApi.common.js', import.meta.url), 'utf8')}
  return exports;
})`)(() => common)

test('REST raw filename hints preserve JSON compatibility and identify original YAML', () => {
  const legacy = rest.transform({ uid: 'legacy', _path: 'legacy.html' })
  assert.equal(legacy._jsonPath, 'legacy.swagger.json')

  const json = rest.transform({ uid: 'json', _path: 'openapi.html', rawExtension: '.json', _raw: '{"openapi":"3.1.0"}' })
  assert.equal(json._jsonPath, 'openapi.swagger.json')
  assert.equal(json._raw, '{"openapi":"3.1.0"}')

  const yaml = rest.transform({ uid: 'yaml', _path: 'openapi.html', rawExtension: '.yaml', _raw: 'openapi: 3.1.0\n' })
  assert.equal(yaml._jsonPath, 'openapi.swagger.yaml')
  assert.equal(yaml._raw, 'openapi: 3.1.0\n')
})

test('REST preserves legacy parameter paths, allOf flattening, and definitions', () => {
  const model = rest.transform({
    uid: 'legacy',
    _path: 'legacy.json',
    children: [{
      uid: 'get',
      operation: 'get',
      path: '/items',
      parameters: [
        { name: 'filter', in: 'query', required: true, schema: { type: 'string' } },
        { name: 'limit', in: 'query', schema: { type: 'integer' } }
      ],
      responses: [{
        schema: {
          'x-internal-ref-name': 'Item',
          allOf: [{ properties: { id: { type: 'integer' } } }, { properties: { name: { type: 'string' } } }]
        },
        examples: [{ mimeType: 'application/json', content: '{"id":1}' }]
      }]
    }]
  })
  const child = model.children[0]
  assert.equal(child.operation, 'GET')
  assert.equal(child.path, '/items?filter[&limit]')
  assert.equal(child._hasSchemaDetails, undefined)
  assert.equal(child.responses[0].examples[0].content, '{\n  "id": 1\n}')
  assert.equal(child.responses[0].schema.cTypeId, 'Item')
  assert.deepEqual(child.responses[0].schema.properties.map(property => property.key), ['id', 'name'])
  assert.equal(child.responses[0].schema.allOf, undefined)
  assert.equal(model.definitions.length, 1)
  assert.equal(model.definitions[0].schemaDetails, undefined)
})

test('REST prepares every request and response media schema and named example', () => {
  const model = rest.transform({
    uid: 'media',
    _path: 'media.json',
    schemas: {},
    children: [{
      uid: 'post',
      operation: 'post',
      path: '/items',
      requestUrl: 'https://api.example.test/v2/items',
      servers: [{ url: 'https://api.example.test/v2' }],
      parameters: [{ name: 'filter', in: 'query', schema: { type: 'string | null', format: 'uuid' } }],
      requestBody: {
        required: true,
        content: [
          { mimeType: 'application/json', schema: { type: 'object' }, examples: [{ name: 'created', content: '{"id":1}' }] },
          { mimeType: 'text/plain', schema: { type: 'string' }, examples: [{ content: 'text request' }] }
        ]
      },
      responses: [{
        statusCode: '200',
        content: [
          { mimeType: 'application/json', schema: { type: 'array', items: { type: 'integer' } }, examples: [{ content: '[1,2]' }] },
          { mimeType: 'text/plain', schema: { type: 'string' }, examples: [{ content: 'text response' }] },
          { mimeType: 'application/octet-stream' }
        ],
        examples: [{ mimeType: 'text/plain', content: 'flattened response' }]
      }, { statusCode: '204', content: [], examples: [{ content: 'must not render' }] }]
    }]
  })
  const child = model.children[0]
  assert.equal(child.path, '/items')
  assert.equal(child.requestUrl, 'https://api.example.test/v2/items')
  assert.equal(child.servers[0].description, '')
  assert.equal(child.parameters[0].schemaDetails.type, 'string | null')
  assert.equal(child.parameters[0].schemaDetails.format, 'uuid')
  assert.equal(child.requestBody.description, '')
  assert.deepEqual(child.requestBody.content.map(media => media.schemaDetails.type), ['object', 'string'])
  assert.deepEqual(child.requestBody.content[0].examples[0], {
    name: 'created', mimeType: 'application/json', content: '{\n  "id": 1\n}'
  })
  assert.equal(child.responses[0].content[0].schemaDetails.items.type, 'integer')
  assert.equal(child.responses[0].content[0].examples[0].content, '[\n  1,\n  2\n]')
  assert.equal(child.responses[0].content[1].examples[0].name, '')
  assert.deepEqual(child.responses[0].content[2].examples, [])
  assert.equal(child.responses[0].content[2].schemaDetails, false)
  assert.equal(child.responses[0].hasContent, true)
  assert.equal(child.responses[1].hasContent, true)
  assert.equal(child.responses[0].examples[0].content, 'flattened response')
})

test('REST keeps nested composition, constraints, unions, boolean schemas, and false enum values', () => {
  const schema = {
    type: 'object',
    required: ['value'],
    properties: {
      value: {
        type: 'string | null',
        description: '<p>A nullable value.</p>',
        constraints: [{ name: 'minLength', value: '0' }],
        enum: ['', null, false, 0]
      },
      list: {
        type: 'array',
        required: true,
        items: {
          composition: [
            { kind: 'All of', schemas: [{ type: 'object', properties: { allowed: { type: 'any value' } } }] },
            { kind: 'One of', schemas: [{ type: 'string' }, { type: 'integer' }] },
            { kind: 'Any of', schemas: [{ type: 'boolean' }, { type: 'null' }] },
            { kind: 'Not', schemas: [{ type: 'no value' }] }
          ]
        }
      }
    }
  }
  const original = structuredClone(schema)
  const model = rest.transform({ uid: 'nested', _path: 'nested.json', schemas: { Nested: schema } })
  const details = model.definitions[0].schemaDetails
  assert.deepEqual(schema, original)
  assert.equal(details.properties[0].required, true)
  assert.equal(details.properties[1].required, true)
  assert.deepEqual(details.properties[0].value.enum, [{ value: '""' }, { value: 'null' }, { value: 'false' }, { value: '0' }])
  assert.deepEqual(details.properties[0].value.constraints, [{ name: 'minLength', value: '0' }])
  assert.equal(details.properties[0].value.description, '<p>A nullable value.</p>')
  const composition = details.properties[1].value.items.composition
  assert.deepEqual(composition.map(item => item.kind), ['All of', 'One of', 'Any of', 'Not'])
  assert.equal(composition[0].schemas[0].properties[0].value.type, 'any value')
  assert.equal(composition[3].schemas[0].type, 'no value')
  assert.deepEqual(composition[3].schemas[0].properties, [])
  assert.equal(composition[3].schemas[0].items, false)
  assert.deepEqual(composition[3].schemas[0].composition, [])
})

test('REST links recursive references and aliases without colliding schema anchors', () => {
  const model = rest.transform({
    uid: 'references',
    _path: 'references.json',
    schemas: {
      'Tree.Node': { type: 'object', properties: { next: { 'x-internal-loop-ref-name': 'Tree.Node' } } },
      Tree_Node: { type: 'any value' },
      Alias: { 'x-internal-ref-name': 'Tree.Node' }
    },
    children: [{
      uid: 'read',
      path: '/tree',
      tags: ['Trees'],
      requestUrl: '/tree',
      responses: [{
        content: [{
          mimeType: 'application/json',
          schema: { type: 'array', items: { 'x-internal-ref-name': 'Tree.Node' } }
        }]
      }]
    }]
  })
  const [tree, distinct, alias] = model.definitions.map(definition => definition.schemaDetails)
  assert.notEqual(tree.id, distinct.id)
  assert.equal(tree.properties[0].value.referenceId, tree.id)
  assert.equal(alias.referenceId, tree.id)
  assert.equal(model.tags[0].children[0].responses[0].content[0].schemaDetails.items.referenceId, tree.id)
  assert.equal(model.definitions.length, 3)
})

test('REST adds inline reference definitions and leaves unresolved references as text', () => {
  const model = rest.transform({
    uid: 'inline',
    _path: 'inline.json',
    children: [{
      uid: 'read',
      path: '/inline',
      requestUrl: '/inline',
      parameters: [{
        schema: {
          type: 'object',
          'x-internal-ref-name': 'Inline',
          properties: { missing: { 'x-internal-loop-ref-name': 'Missing' } }
        }
      }]
    }]
  })
  const details = model.children[0].parameters[0].schemaDetails
  assert.equal(details.referenceId, model.definitions[0].schemaDetails.id)
  assert.equal(details.properties[0].value.referenceName, 'Missing')
  assert.equal(details.properties[0].value.referenceId, '')
})

test('REST renders parameter content and keeps same-name external schema references distinct', () => {
  const model = rest.transform({
    uid: 'parameters',
    _path: 'parameters.json',
    children: [{
      uid: 'search',
      path: '/items',
      parameters: [{
        name: 'filter',
        in: 'query',
        default: '{"active":true}',
        content: [
          {
            mimeType: 'application/json',
            schema: {
              type: 'object',
              'x-internal-ref-name': 'models/first.yaml#Filter',
              properties: { next: { 'x-internal-loop-ref-name': 'models/first.yaml#Filter' } }
            },
            examples: [{ name: 'active', content: '{"active":true}' }]
          },
          {
            mimeType: 'text/plain',
            schema: { type: 'string', 'x-internal-ref-name': 'models/second.yaml#Filter' },
            examples: [{ content: 'active' }]
          }
        ]
      }]
    }]
  })
  const parameter = model.children[0].parameters[0]
  assert.equal(parameter.hasContent, true)
  assert.equal(parameter.schemaDetails, false)
  assert.equal(parameter.default, '{"active":true}')
  assert.equal(parameter.content[0].exampleDetails[0].content, '{\n  "active": true\n}')
  assert.equal(parameter.content[0].exampleDetails[0].name, 'active')
  assert.equal(parameter.content[1].schemaDetails.type, 'string')
  const [first, second] = model.definitions.map(definition => definition.schemaDetails)
  assert.notEqual(first.id, second.id)
  assert.equal(parameter.content[0].schemaDetails.referenceId, first.id)
  assert.equal(parameter.content[1].schemaDetails.referenceId, second.id)
  assert.equal(first.properties[0].value.referenceId, first.id)
})

test('REST renders schema examples without inheriting names, MIME types, or ancestor examples', () => {
  const schema = {
    type: 'object',
    examples: [{ content: '{"state":"active"}' }, { content: '' }],
    properties: { state: { type: 'string', enum: ['active', 'archived'], examples: [{ content: '"active"' }] } },
    items: { type: 'string' }
  }
  const original = structuredClone(schema)
  const model = rest.transform({ uid: 'examples', _path: 'examples.json', schemas: { Example: schema } })
  const details = model.definitions[0].schemaDetails
  assert.deepEqual(schema, original)
  assert.deepEqual(details.exampleDetails[0], {
    name: '',
    mimeType: '',
    content: '{"state":"active"}',
    hasContent: true,
    externalValue: '',
    externalHref: ''
  })
  assert.equal(details.exampleDetails[1].hasContent, true)
  assert.equal(details.exampleDetails[1].content, '')
  assert.equal(details.properties[0].value.exampleDetails[0].content, '"active"')
  assert.deepEqual(details.properties[0].value.enum, [{ value: '"active"' }, { value: '"archived"' }])
  assert.deepEqual(details.items.exampleDetails, [])
})

test('REST displays external example URLs without inventing content or linking executable schemes', () => {
  const urls = ['https://example.test/sample.json', 'http://example.test/sample.json', 'samples/local.json', 'javascript:alert(1)']
  const model = rest.transform({
    uid: 'external-examples',
    _path: 'external-examples.json',
    children: [{
      uid: 'read',
      path: '/items',
      responses: [{
        content: [{
          mimeType: 'application/json',
          examples: urls.map(externalValue => ({ name: 'external', externalValue, content: null }))
        }]
      }]
    }]
  })
  const examples = model.children[0].responses[0].content[0].exampleDetails
  assert.deepEqual(examples.map(example => example.externalValue), urls)
  assert.deepEqual(examples.map(example => example.externalHref), [...urls.slice(0, 2), '', ''])
  assert.ok(examples.every(example => example.name === 'external' && example.content === '' && !example.hasContent))
})

for (const flagLocation of ['root', 'operation']) {
  test(`REST preserves literal enum, examples, and extensions with the ${flagLocation} feature flag`, () => {
    const literal = {
      description: 'literal **description**, not markup',
      allOf: [{ type: 'string' }, { properties: { literal: { type: 'integer' } } }],
      $ref: '#/literal/value',
      schema: { properties: { description: { type: 'string' } } },
      'x-internal-ref-name': 'not-a-definition'
    }
    const original = structuredClone(literal)
    const schema = {
      type: 'object',
      enum: [literal],
      examples: [{ content: JSON.stringify(literal), 'x-literal': structuredClone(literal) }],
      constraints: [{ name: 'const', value: JSON.stringify(literal) }],
      properties: { value: { type: 'string' } },
      'x-schema-shaped': structuredClone(literal)
    }
    const originalSchema = structuredClone(schema)
    const operation = {
      uid: 'read',
      path: '/literal',
      _preserveLiteralData: flagLocation === 'operation',
      parameters: [{ name: 'filter', in: 'query', required: true, schema }],
      responses: [{
        schema: { type: 'object', enum: [structuredClone(literal)] },
        examples: [{ mimeType: 'text/plain', content: JSON.stringify(literal), 'x-literal': structuredClone(literal) }]
      }],
      'x-operation': structuredClone(literal)
    }
    const model = rest.transform({
      uid: 'literal',
      _path: 'literal.json',
      _preserveLiteralData: flagLocation === 'root',
      'x-root': structuredClone(literal),
      children: [operation]
    })
    assert.equal(operation._hasSchemaDetails, true)
    assert.equal(operation.path, '/literal')
    assert.deepEqual(schema, originalSchema)
    assert.deepEqual(model['x-root'], original)
    assert.deepEqual(operation['x-operation'], original)
    assert.deepEqual(operation.responses[0].schema.enum[0], original)
    assert.deepEqual(operation.responses[0].examples[0]['x-literal'], original)
    assert.equal(operation.responses[0].examples[0].content, JSON.stringify(original))
    assert.deepEqual(operation.parameters[0].schemaDetails.enum, [{ value: JSON.stringify(original) }])
    assert.equal(operation.parameters[0].schemaDetails.exampleDetails[0].content, JSON.stringify(original))
    assert.deepEqual(model.definitions, [])
  })
}
