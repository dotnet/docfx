// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import test from 'node:test'
import assert from 'node:assert'
import { readFileSync } from 'node:fs'
import { createRequire } from 'node:module'
import lunr from 'lunr'
import stemmer from 'lunr-languages/lunr.stemmer.support'
import multi from 'lunr-languages/lunr.multi'
import { createIndex, SearchHit } from './search-index'
import { loadLanguages } from './search-languages'
import { patchLunrLanguage } from '../../lunr-languages.js'

const require = createRequire(import.meta.url)

/** The parts of a language pack that lunr-languages adds to lunr */
type Language = {
  stemmer: (token: lunr.Token) => lunr.Token,
  stopWordFilter: (token: lunr.Token) => lunr.Token | undefined,
}

const language = (lang: string) => (lunr as unknown as Record<string, Language>)[lang]

const token = (word: string) => new lunr.Token(word, {})

const source = (lang: string) => readFileSync(require.resolve(`lunr-languages/lunr.${lang}.js`), 'utf8')

type LanguagePack = { default: (instance: typeof lunr) => void }

/**
 * Evaluates a CommonJS module in strict mode, which is how it ends up in the ES module bundle.
 * The UMD wrapper of a language pack assigns its init function to `module.exports`.
 */
function loadStrict(source: string): LanguagePack {
  const module = { exports: null as unknown }
  // The only way to run code as strict as an ES module is to compile it as such
  // eslint-disable-next-line no-new-func
  new Function('module', 'exports', `"use strict";\n${source}`)(module, module.exports)
  return { default: module.exports as LanguagePack['default'] }
}

/** The Arabic and Hebrew packs, patched by build.js before they are bundled */
const patched: Record<string, () => Promise<LanguagePack>> = {
  ar: async() => loadStrict(patchLunrLanguage(source('ar'))!),
  he: async() => loadStrict(patchLunrLanguage(source('he'))!)
}

const docs: Record<string, SearchHit> = {
  'ar.html': { href: 'ar.html', title: 'الكتاب', summary: 'الكلاب في المدرسة', keywords: '' },
  'en.html': { href: 'en.html', title: 'docfx', summary: 'The installer is running', keywords: '' },
  // A language without a language pack
  'fa.html': { href: 'fa.html', title: 'شروع به کار', summary: 'برنامه‌نویسی', keywords: '' },
}

const refs = (index: lunr.Index, query: string) => index.search(query).map(result => result.ref).sort()

/** Runs `action` with the given console methods replaced by collectors, and returns its result and what they logged */
async function collectConsole<T>(methods: ('warn' | 'error')[], action: () => Promise<T>) {
  const original = Object.fromEntries(methods.map(method => [method, console[method]]))
  const messages: string[] = []
  methods.forEach(method => { console[method] = (message: string) => { messages.push(`${method}: ${message}`) } })
  try {
    return { result: await action(), messages }
  } finally {
    Object.assign(console, original)
  }
}

test('the Arabic and Hebrew stemmers need the global object as their state', () => {
  stemmer(lunr)

  // Bundled as an ES module the code is strict, where `this` is undefined
  for (const lang of ['ar', 'he']) {
    assert.throws(() => loadStrict(source(lang)).default(lunr), /Cannot set properties of undefined \(setting 'result'\)/)
  }

  // There is nothing to patch once lunr-languages fixes its stemmers
  assert.strictEqual(patchLunrLanguage('var self = {};'), null)
  assert.strictEqual(patchLunrLanguage('var x = 1;'), null)
  assert.strictEqual(patchLunrLanguage('var self = this;'), 'var self = {};')
})

test('the patched stemmers work in strict mode and keep their state apart', async() => {
  stemmer(lunr)

  // lunr warns about the trimmers that the test above has registered already
  await collectConsole(['warn'], async() => {
    for (const lang of ['ar', 'he']) {
      (await patched[lang]()).default(lunr)
    }
  })

  // Arabic words are stemmed to their root
  assert.strictEqual(language('ar').stemmer(token('الكتاب')).toString(), 'كتاب')
  assert.strictEqual(language('ar').stemmer(token('يقرأون')).toString(), 'قرا')

  // Loading Hebrew used to replace the state of the Arabic stemmer
  assert.strictEqual(language('ar').stemmer(token('الكتاب')).toString(), 'كتاب')
  assert.strictEqual(language('he').stemmer(token('ספר')).toString(), 'ספר')

  // Words in other scripts are left alone
  assert.strictEqual(language('ar').stemmer(token('docfx')).toString(), 'docfx')
  assert.strictEqual(language('ar').stemmer(token('שלום')).toString(), 'שלום')

  // The stop words of both languages are removed
  assert.strictEqual(language('ar').stopWordFilter(token('في')), undefined)
  assert.strictEqual(language('ar').stopWordFilter(token('الكتاب'))?.toString(), 'الكتاب')
  assert.strictEqual(language('he').stopWordFilter(token('את')), undefined)
  assert.strictEqual(language('he').stopWordFilter(token('ספר'))?.toString(), 'ספר')
})

test('languages without a language pack are skipped with a warning', async() => {
  // Persian has no language pack: its words are analyzed like English words
  const { result, messages } = await collectConsole(['warn'], () => loadLanguages(lunr, ['fa'], {}))
  assert.deepStrictEqual(result, [])
  assert.deepStrictEqual(messages, ['warn: docfx: Search does not support the language "fa", its words are analyzed like English words.'])

  // English is built into lunr
  assert.deepStrictEqual((await collectConsole(['warn'], () => loadLanguages(lunr, ['en'], {}))).messages, [])

  // Other languages are still loaded
  const other = await collectConsole(['warn'], () => loadLanguages(lunr, ['fa', 'en'], {}))
  assert.deepStrictEqual(other.result, ['en'])
  assert.strictEqual(other.messages.length, 1)
})

test('a language pack that fails to load does not disable the other languages', async() => {
  const loaders: Record<string, () => Promise<LanguagePack>> = {
    ar: async() => { throw new Error('cannot download') },
    he: async() => ({ default: () => { throw new Error('unsupported') } })
  }

  const { result, messages } = await collectConsole(['error'], () => loadLanguages(lunr, ['ar', 'he', 'en'], loaders))
  assert.deepStrictEqual(result, ['en'])
  assert.deepStrictEqual(messages.map(message => message.split(', ')[0]), [
    'error: docfx: Cannot load the search language "ar"',
    'error: docfx: Cannot load the search language "he"'
  ])
})

test('the index analyzes the words of every loaded language', async() => {
  multi(lunr)
  stemmer(lunr)

  // lunr warns about the stemmers that the test above has registered already
  const { result: languages, messages } = await collectConsole(['warn'], () => loadLanguages(lunr, ['en', 'ar', 'he'], patched))
  assert.deepStrictEqual(languages, ['en', 'ar', 'he'])
  assert.deepStrictEqual(messages.filter(message => message.includes('docfx:')), [])

  const index = createIndex(docs, languages)

  // Arabic words are stemmed, so that a search for "book" finds "the book"
  assert.deepStrictEqual(refs(index, '+كتاب'), ['ar.html'])
  assert.deepStrictEqual(refs(index, '+مدرسة'), ['ar.html'])

  // English words are stemmed too
  assert.deepStrictEqual(refs(index, '+docfx'), ['en.html'])
  assert.deepStrictEqual(refs(index, '+runs'), ['en.html'])

  // Words in a language that is not configured are indexed as they are written
  assert.deepStrictEqual(refs(index, '+شروع'), ['fa.html'])
  assert.deepStrictEqual(refs(index, '+نویسی'), ['fa.html'])
})

test('the words of other languages are indexed when one language is configured', async() => {
  multi(lunr)
  stemmer(lunr)

  // lunr's multi-language trimmer would keep Arabic characters only
  const languages = await loadLanguages(lunr, ['ar'], patched)
  const index = createIndex(docs, languages)

  assert.deepStrictEqual(refs(index, '+كتاب'), ['ar.html'])
  assert.deepStrictEqual(refs(index, '+docfx'), ['en.html'])
  assert.deepStrictEqual(refs(index, '+شروع'), ['fa.html'])
})
