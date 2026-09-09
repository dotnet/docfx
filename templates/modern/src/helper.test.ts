// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import test from 'node:test'
import assert from 'node:assert'
import { breakWord, isExternalHref, isSameURL } from './helper'

test('break text', () => {
  assert.deepStrictEqual(breakWord('Other APIs'), ['Other APIs'])
  assert.deepStrictEqual(breakWord('System.CodeDom'), ['System.', 'Code', 'Dom'])
  assert.deepStrictEqual(breakWord('System.Collections.Dictionary<string, object>'), ['System.', 'Collections.', 'Dictionary<', 'string,', ' object>'])
  assert.deepStrictEqual(breakWord('https://github.com/dotnet/docfx'), ['https://github.', 'com/', 'dotnet/', 'docfx'])
})

test('is same URL', () => {
  assert.ok(isSameURL({ pathname: '/' }, { pathname: '/' }))
  assert.ok(isSameURL({ pathname: '/index.html' }, { pathname: '/' }))
  assert.ok(isSameURL({ pathname: '/a/index.html' }, { pathname: '/a' }))
  assert.ok(isSameURL({ pathname: '/a/index.html' }, { pathname: '/a/' }))
  assert.ok(isSameURL({ pathname: '/a' }, { pathname: '/a/' }))
  assert.ok(isSameURL({ pathname: '/a/foo.html' }, { pathname: '/a/foo' }))
  assert.ok(isSameURL({ pathname: '/a/foo/' }, { pathname: '/a/foo' }))
  assert.ok(isSameURL({ pathname: '/a/foo/index.html' }, { pathname: '/a/foo' }))
  assert.ok(isSameURL({ pathname: '/a/index.html' }, { pathname: '/A/Index.html' }))

  assert.ok(!isSameURL({ pathname: '/a/foo/index.html' }, { pathname: '/a/bar' }))
})

test('is external URL', () => {
  const originalWindow = Object.getOwnPropertyDescriptor(globalThis, 'window')
  Object.defineProperty(globalThis, 'window', {
    configurable: true,
    value: { location: new URL('https://localhost:8080/') }
  })

  try {
    assert.ok(!isExternalHref(new URL('https://localhost:8080/docs')))
    assert.ok(isExternalHref(new URL('https://localhost:1234/docs')))
    assert.ok(isExternalHref(new URL('http://localhost:8080/docs')))
    assert.ok(isExternalHref(new URL('https://example.com:8080/docs')))
  } finally {
    if (originalWindow) {
      Object.defineProperty(globalThis, 'window', originalWindow)
    } else {
      Reflect.deleteProperty(globalThis, 'window')
    }
  }
})
