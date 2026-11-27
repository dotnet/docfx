// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import test from 'node:test'
import assert from 'node:assert'
import { breakWord, isCursiveScript, isExternalHref, isRtl, isSameURL } from './helper'

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

test('is right-to-left', () => {
  const original = Object.getOwnPropertyDescriptor(globalThis, 'document')
  const setDirection = (dir: string) => Object.defineProperty(globalThis, 'document', {
    configurable: true,
    value: { documentElement: { dir } }
  })

  try {
    setDirection('rtl')
    assert.ok(isRtl())

    setDirection('ltr')
    assert.ok(!isRtl())

    // Pages without a direction are left-to-right
    setDirection('')
    assert.ok(!isRtl())
  } finally {
    if (original) {
      Object.defineProperty(globalThis, 'document', original)
    } else {
      Reflect.deleteProperty(globalThis, 'document')
    }
  }
})

test('is cursive script', () => {
  // Arabic script joins its letters: Arabic, Persian, Urdu, Divehi
  assert.ok(isCursiveScript('محتوى المقال'))
  assert.ok(isCursiveScript('در این مقاله'))
  assert.ok(isCursiveScript('اس مضمون میں'))
  assert.ok(isCursiveScript('މި ލިނުގެ'))
  assert.ok(isCursiveScript('In this article, مقاله'))

  // Other scripts are set apart letter by letter
  assert.ok(!isCursiveScript('In this article'))
  assert.ok(!isCursiveScript('本文内容'))
  // Hebrew is right-to-left, but its letters are not joined
  assert.ok(!isCursiveScript('על המאמר הזה'))
  assert.ok(!isCursiveScript(''))
  assert.ok(!isCursiveScript(undefined))
})
