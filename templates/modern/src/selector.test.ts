// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import test from 'node:test'
import assert from 'node:assert'
import { dedupeEntries, findCurrentEntry, findTargetEntry, monikerSectionApplies, normalizeUrl, parseEntries, parseMonikers, resolveEntryUrl, SiteEntry } from './selector'

const manifest = [
  { label: 'main', url: '/repo/main/' },
  { label: 'v2.0 (stable)', url: '/repo/' },
  { label: 'v1.0', url: '/repo/v1.0/' },
]

const entries: SiteEntry[] = parseEntries(manifest, 'https://example.com/repo/versions.json')

test('normalize url', () => {
  assert.strictEqual(normalizeUrl('/'), '/')
  assert.strictEqual(normalizeUrl('/repo/v2.0'), '/repo/v2.0/')
  assert.strictEqual(normalizeUrl('/repo/v2.0/'), '/repo/v2.0/')
})

test('parse entries', () => {
  assert.deepStrictEqual(entries, [
    { label: 'main', url: 'https://example.com/repo/main/' },
    { label: 'v2.0 (stable)', url: 'https://example.com/repo/' },
    { label: 'v1.0', url: 'https://example.com/repo/v1.0/' },
  ])

  // Entry URLs are resolved against the manifest URL
  assert.deepStrictEqual(
    parseEntries([{ label: 'main', url: 'main/' }, { label: 'stable', url: '../' }], 'https://example.com/repo/versions.json'),
    [
      { label: 'main', url: 'https://example.com/repo/main/' },
      { label: 'stable', url: 'https://example.com/' },
    ])

  // Entries without a label or a URL are ignored
  assert.deepStrictEqual(parseEntries([{ label: 'x' }, { url: '/x/' }, { label: '', url: '/x/' }, 'x'], 'https://example.com/'), [])
  assert.deepStrictEqual(parseEntries({}, 'https://example.com/'), [])
})

test('find current entry', () => {
  assert.strictEqual(findCurrentEntry(entries, 'https://example.com/repo/main/a.html'), entries[0])
  assert.strictEqual(findCurrentEntry(entries, 'https://example.com/repo/v1.0/a.html'), entries[2])

  // Longest prefix wins, the stable version is served from the site root
  assert.strictEqual(findCurrentEntry(entries, 'https://example.com/repo/index.html'), entries[1])
  assert.strictEqual(findCurrentEntry(entries, 'https://example.com/repo/'), entries[1])

  // Query strings and fragments are part of the page URL
  assert.strictEqual(findCurrentEntry(entries, 'https://example.com/repo/main/a.html?q=1#x'), entries[0])

  // A prefix must not match partially, /repo/v1.0/ does not match /repo/v1.0.2/
  assert.strictEqual(findCurrentEntry(entries, 'https://example.com/repo/v1.0.2/a.html'), entries[1])

  assert.strictEqual(findCurrentEntry([], 'https://example.com/'), undefined)
})

test('dedupe entries', () => {
  // A version or locale hosted under multiple URLs is displayed once
  assert.deepStrictEqual(
    dedupeEntries([
      { label: 'English', url: 'https://example.com/repo/main/en/' },
      { label: 'English', url: 'https://example.com/repo/v2.0/en/' },
      { label: '日本語', url: 'https://example.com/repo/main/ja/' },
    ]),
    [
      { label: 'English', url: 'https://example.com/repo/main/en/' },
      { label: '日本語', url: 'https://example.com/repo/main/ja/' },
    ])
})

test('find target entry', () => {
  const candidates = [
    { label: 'English', url: 'https://example.com/repo/main/en/' },
    { label: 'English', url: 'https://example.com/repo/v2.0/en/' },
  ]

  // Switching locale keeps the version segment of the current page
  assert.strictEqual(findTargetEntry(candidates, 'https://example.com/repo/v2.0/ja/a.html'), candidates[1])
  assert.strictEqual(findTargetEntry(candidates, 'https://example.com/repo/main/ja/a.html'), candidates[0])

  // Falls back to the first entry when nothing matches
  assert.strictEqual(findTargetEntry(candidates, 'https://example.com/other/ja/a.html'), candidates[0])
})

test('resolve entry url', () => {
  assert.strictEqual(
    resolveEntryUrl(entries[0], entries[2], 'https://example.com/repo/main/a.html'),
    'https://example.com/repo/v1.0/a.html')

  // Query strings and fragments are preserved
  assert.strictEqual(
    resolveEntryUrl(entries[0], entries[2], 'https://example.com/repo/main/a.html?q=1#x'),
    'https://example.com/repo/v1.0/a.html?q=1#x')

  // The current page is not under the current entry's URL
  assert.strictEqual(resolveEntryUrl(entries[0], entries[2], 'https://example.com/other/a.html'), null)
  assert.strictEqual(resolveEntryUrl(undefined, entries[2], 'https://example.com/repo/main/a.html'), null)
})

test('parse monikers', () => {
  assert.deepStrictEqual(parseMonikers('net-5.0'), ['net-5.0'])
  assert.deepStrictEqual(parseMonikers('net-5.0 || net-7.0'), ['net-5.0', 'net-7.0'])
  assert.deepStrictEqual(parseMonikers('net-5.0 net-7.0'), ['net-5.0', 'net-7.0'])
  assert.deepStrictEqual(parseMonikers(''), [])
})

test('moniker section applies', () => {
  // A section applies when it shares a moniker with the version
  assert.ok(monikerSectionApplies('net-5.0', 'net-5.0 net-7.0'))
  assert.ok(monikerSectionApplies('net-5.0 || net-7.0', 'net-7.0'))
  assert.ok(!monikerSectionApplies('net-5.0', 'net-7.0'))

  // Sections without monikers always apply
  assert.ok(monikerSectionApplies('', 'net-7.0'))

  // Versions without monikers keep every section visible
  assert.ok(monikerSectionApplies('net-5.0', ''))
})
