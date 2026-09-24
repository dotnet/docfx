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

test('REST preserves query paths and renders allOf without flattening schemas', () => {
  const schema = {
    'x-internal-ref-name': 'Item',
    allOf: [
      { properties: { id: { type: 'integer' } } },
      { required: ['name'], properties: { name: { type: 'string' } } }
    ]
  }
  const original = structuredClone(schema)
  const model = rest.transform({
    uid: 'legacy',
    _path: 'legacy.html',
    children: [{
      uid: 'get',
      operation: 'get',
      path: '/items',
      parameters: [{ name: 'filter', in: 'query', required: true }, { name: 'limit', in: 'query' }],
      responses: [{ schema, examples: [{ mimeType: 'application/json', content: '{"id":1}' }] }]
    }]
  })
  const child = model.children[0]
  assert.equal(model._jsonPath, 'legacy.swagger.json')
  assert.equal(child.operation, 'GET')
  assert.equal(child.path, '/items?filter[&limit]')
  assert.equal(child.responses[0].examples[0].content, '{\n  "id": 1\n}')
  const details = child.responses[0].schemaDetails
  assert.equal(details.referenceId, 'Item')
  assert.deepEqual(details.composition[0].schemas.map(branch => branch.properties.map(property => property.key)), [['id'], ['name']])
  assert.equal(details.composition[0].schemas[1].properties[0].required, true)
  assert.deepEqual(schema, original)
  assert.equal(model.definitions.length, 1)
  assert.equal(model.definitions[0].schemaDetails.id, 'Item')
})

test('REST resolves recursive links after collecting schemas from all operations', () => {
  const model = rest.transform({
    uid: 'references',
    _path: 'references.html',
    children: [{
      uid: 'list',
      tags: ['Trees'],
      responses: [{ schema: { type: 'array', items: { 'x-internal-loop-ref-name': 'Tree.Node' } } }]
    }, {
      uid: 'create',
      tags: ['Trees'],
      parameters: [{
        schema: {
          type: 'object',
          'x-internal-ref-name': 'Tree.Node',
          properties: {
            next: { 'x-internal-loop-ref-name': 'Tree.Node' },
            missing: { 'x-internal-loop-ref-name': 'Missing' }
          }
        }
      }]
    }]
  })
  const [list, create] = model.tags[0].children
  assert.equal(list.responses[0].schemaDetails.items.referenceId, 'Tree_Node')
  assert.equal(create.parameters[0].schemaDetails.properties[0].value.referenceId, 'Tree_Node')
  assert.equal(create.parameters[0].schemaDetails.properties[1].value.referenceName, 'Missing')
  assert.equal(create.parameters[0].schemaDetails.properties[1].value.referenceId, '')
  assert.equal(model.definitions.length, 1)
  assert.equal(model.definitions[0].schemaDetails.referenceName, '')
})

test('REST preserves schema-shaped literals and prepares missing fields for Mustache scopes', () => {
  const literal = {
    description: '**literal**',
    allOf: [{ type: 'string' }],
    'x-internal-ref-name': 'NotADefinition'
  }
  const schema = {
    type: 'object',
    description: '<p>Schema description.</p>',
    example: literal,
    properties: {
      state: { type: 'string', enum: ['', 'active'] },
      active: { type: 'boolean', enum: [false] },
      count: { type: 'integer', enum: [0] },
      empty: { type: 'string', example: '' }
    },
    'x-literal': literal
  }
  const original = structuredClone(schema)
  const model = rest.transform({
    uid: 'literal',
    _path: 'literal.html',
    children: [{ uid: 'get', responses: [{ schema }] }]
  })
  const details = model.children[0].responses[0].schemaDetails
  assert.deepEqual(schema, original)
  assert.equal(details.exampleDetails[0].content, JSON.stringify(literal))
  assert.equal(details.exampleDetails[0].mimeType, '')
  assert.equal(details.properties[0].value.description, '')
  assert.equal(details.properties[0].value.items, false)
  assert.deepEqual(details.properties[0].value.exampleDetails, [])
  assert.deepEqual(details.properties[0].value.enum, [{ value: '""' }, { value: '"active"' }])
  assert.deepEqual(details.properties[1].value.enum, [{ value: 'false' }])
  assert.deepEqual(details.properties[2].value.enum, [{ value: '0' }])
  assert.equal(details.properties[3].value.exampleDetails[0].content, '""')
  assert.deepEqual(model.definitions, [])
})
