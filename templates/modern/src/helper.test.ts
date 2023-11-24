// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { breakWord, isSameURL } from './helper'

test('break text', () => {
  expect(breakWord('Other APIs')).toEqual(['Other APIs'])
  expect(breakWord('System.CodeDom')).toEqual(['System.', 'Code', 'Dom'])
  expect(breakWord('System.Collections.Dictionary<string, object>')).toEqual(['System.', 'Collections.', 'Dictionary<', 'string,', ' object>'])
  expect(breakWord('https://github.com/dotnet/docfx')).toEqual(['https://github.', 'com/', 'dotnet/', 'docfx'])
})

test('is same URL', () => {
  expect(isSameURL({ pathname: '/' }, { pathname: '/' })).toBeTruthy()
  expect(isSameURL({ pathname: '/index.html' }, { pathname: '/' })).toBeTruthy()
  expect(isSameURL({ pathname: '/a/index.html' }, { pathname: '/a' })).toBeTruthy()
  expect(isSameURL({ pathname: '/a/index.html' }, { pathname: '/a/' })).toBeTruthy()
  expect(isSameURL({ pathname: '/a' }, { pathname: '/a/' })).toBeTruthy()
  expect(isSameURL({ pathname: '/a/foo.html' }, { pathname: '/a/foo' })).toBeTruthy()
  expect(isSameURL({ pathname: '/a/foo/' }, { pathname: '/a/foo' })).toBeTruthy()
  expect(isSameURL({ pathname: '/a/foo/index.html' }, { pathname: '/a/foo' })).toBeTruthy()

  expect(isSameURL({ pathname: '/a/foo/index.html' }, { pathname: '/a/bar' })).toBeFalsy()
})
