// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import test from 'node:test'
import assert from 'node:assert'
import lunr from 'lunr'
import { createIndex, SearchHit, separator, trimmer } from './search-index'

const trim = (word: string) => trimmer(new lunr.Token(word, {})).toString()

const hit = (href: string, title: string, summary = ''): SearchHit => ({ href, title, summary, keywords: '' })

const docs = {
  'fa.html': hit('fa.html', 'شروع به کار', 'برای نصب docfx دستور زیر را اجرا کنید. برنامه‌نویسی'),
  'en.html': hit('en.html', 'Introduction', 'This process runs the installer'),
  'ru.html': hit('ru.html', 'Привет', 'Здравствуйте'),
  'ja.html': hit('ja.html', 'はじめてのガイド', 'docfx のインストール'),
}

const refs = (index: lunr.Index, query: string) => index.search(query).map(result => result.ref).sort()

test('the trimmer keeps the letters and digits of every script', () => {
  assert.strictEqual(trim('سلام'), 'سلام')
  assert.strictEqual(trim('Привет'), 'Привет')
  assert.strictEqual(trim('站台'), '站台')

  // Combining marks stay attached to the word, e.g. vowel signs and diacritics
  assert.strictEqual(trim('ที่'), 'ที่')
  assert.strictEqual(trim('كِتَابٌ'), 'كِتَابٌ')

  // Punctuation around a word is removed, underscores like in `__init__` are kept
  assert.strictEqual(trim('"hello,"'), 'hello')
  assert.strictEqual(trim('...(docfx)'), 'docfx')
  assert.strictEqual(trim('__init__'), '__init__')
  assert.strictEqual(trim('---'), '')
})

test('words are separated by punctuation and by the zero-width non-joiner', () => {
  lunr.tokenizer.separator = separator
  assert.deepStrictEqual(
    lunr.tokenizer('برنامه‌نویسی می‌شود docfx --version').map(String),
    ['برنامه', 'نویسی', 'می', 'شود', 'docfx', 'version'])
})

test('the default pipeline indexes the words of languages without a language pack', () => {
  const index = createIndex(docs)

  assert.deepStrictEqual(refs(index, '+شروع'), ['fa.html'])
  assert.deepStrictEqual(refs(index, '+نصب'), ['fa.html'])
  assert.deepStrictEqual(refs(index, '+Привет'), ['ru.html'])

  // Text that is not separated by spaces is indexed by its whitespace separated parts
  assert.deepStrictEqual(refs(index, '+のインストール'), ['ja.html'])

  // The words of a Persian text that are joined by a zero-width non-joiner are found by their parts
  assert.deepStrictEqual(refs(index, '+نویسی'), ['fa.html'])
})

test('the default pipeline still removes English stop words and stems English words', () => {
  const index = createIndex(docs)

  assert.deepStrictEqual(refs(index, '+running'), ['en.html'])
  assert.deepStrictEqual(refs(index, '+installers'), ['en.html'])
  assert.deepStrictEqual(refs(index, '+the'), [])
})

test('English alone keeps the default pipeline', () => {
  const index = createIndex(docs, ['en'])

  assert.deepStrictEqual(refs(index, '+running'), ['en.html'])
  assert.deepStrictEqual(refs(index, '+شروع'), ['fa.html'])
})

test('an index can be saved and loaded again', () => {
  const index = createIndex(docs)
  const loaded = lunr.Index.load(JSON.parse(JSON.stringify(index.toJSON())))

  assert.deepStrictEqual(refs(loaded, '+شروع'), ['fa.html'])
  assert.deepStrictEqual(refs(loaded, '+installers'), ['en.html'])
})
