// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import test from 'node:test'
import assert from 'node:assert'
import { dedupeEntries, findCurrentEntry, findTargetEntry, monikerSectionApplies, normalizeUrl, parseEntries, parseMonikerNames, parseMonikers, resolveDestination, resolveEntryUrl, resolveSelectedMoniker, walkUpToExisting, SiteEntry } from './selector'

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

test('parse moniker names', () => {
  assert.deepStrictEqual(parseMonikerNames('net-5.0 || net-7.0'), ['net-5.0', 'net-7.0'])
  assert.deepStrictEqual(parseMonikerNames('>= net-5.0 && < net-7.0'), ['net-5.0', 'net-7.0'])
  assert.deepStrictEqual(parseMonikerNames('net-5.0 || net-5.0'), ['net-5.0'])
  assert.deepStrictEqual(parseMonikerNames(''), [])
})

test('resolve selected moniker', () => {
  const monikers = ['net-8.0', 'net-9.0']

  // The moniker picked by the reader wins when it applies to the page
  assert.strictEqual(resolveSelectedMoniker(monikers, 'net-9.0', null), 'net-9.0')
  assert.strictEqual(resolveSelectedMoniker(monikers, 'net-8.0', 'net-9.0'), 'net-8.0')

  // Falls back to the moniker declared by the current version
  assert.strictEqual(resolveSelectedMoniker(monikers, null, 'net-8.0'), 'net-8.0')
  assert.strictEqual(resolveSelectedMoniker(monikers, 'net-7.0', 'net-8.0'), 'net-8.0')

  // No filtering when neither a stored pick nor a declared range applies
  assert.strictEqual(resolveSelectedMoniker(monikers, null, null), null)
  assert.strictEqual(resolveSelectedMoniker(monikers, 'net-7.0', null), null)

  // A stored pick that doesn't cover a moniker of the page is ignored
  assert.strictEqual(resolveSelectedMoniker(['net-8.0'], 'net-9.0', 'net-9.0'), 'net-9.0')
})

test('resolve selected moniker with a declared moniker range', () => {
  const monikers = ['net-8.0', 'net-9.0', 'net-10.0']

  // A declared range that covers at least one moniker of the page is used as the default filter
  assert.strictEqual(resolveSelectedMoniker(monikers, null, 'net-8.0 net-9.0'), 'net-8.0 net-9.0')
  assert.strictEqual(resolveSelectedMoniker(monikers, null, 'net-8.0 || net-9.0'), 'net-8.0 || net-9.0')
  assert.strictEqual(resolveSelectedMoniker(monikers, null, 'net-8.0 net-11.0'), 'net-8.0 net-11.0')

  // A declared range applies even when it covers no moniker of the page,
  // so that sections of other versions stay hidden
  assert.strictEqual(resolveSelectedMoniker(['net-11.0'], null, 'net-8.0 net-9.0'), 'net-8.0 net-9.0')

  // The moniker picked by the reader wins over the declared range
  assert.strictEqual(resolveSelectedMoniker(monikers, 'net-10.0', 'net-8.0 net-9.0'), 'net-10.0')
})

test('walk up to existing', async() => {
  // The locale-preserving walk-up: when a page is missing in the target version,
  // the nearest existing ancestor is used so that other segments are preserved
  const exists = new Set(['/repo/main/en/', '/repo/main/', '/repo/'])
  const probe = url => Promise.resolve(exists.has(url.toLowerCase()))

  assert.strictEqual(await walkUpToExisting('/repo/main/', '/repo/main/en/only-en.html', probe), '/repo/main/en/')
  assert.strictEqual(await walkUpToExisting('/repo/main/', '/repo/main/en/a/b/page.html', probe), '/repo/main/en/')
  assert.strictEqual(await walkUpToExisting('/repo/main/', '/repo/main/page.html', probe), '/repo/main/')

  // Stops at the target home page when intermediate directories don't exist
  const sparse = new Set(['/repo/'])
  assert.strictEqual(await walkUpToExisting('/repo/main/', '/repo/main/en/only-en.html', url => Promise.resolve(sparse.has(url.toLowerCase()))), '/repo/main/')

  // Falls back to the target home page when nothing exists
  assert.strictEqual(await walkUpToExisting('/repo/main/', '/repo/main/en/only-en.html', () => Promise.resolve(false)), '/repo/main/')

  // Query strings and fragments are ignored
  assert.strictEqual(await walkUpToExisting('/repo/main/', '/repo/main/en/page.html?q=1#x', probe), '/repo/main/en/')
})

test('resolve destination', async() => {
  const current = entries[0]
  const target = entries[2]
  const exists = new Set(['https://example.com/repo/v1.0/a.html', 'https://example.com/repo/v1.0/en/'])
  const probe = url => Promise.resolve(exists.has(url))

  // The same page exists in the target version
  assert.strictEqual(
    await resolveDestination(current, target, 'https://example.com/repo/main/a.html', probe),
    'https://example.com/repo/v1.0/a.html')

  // The same page is missing: walk up to the nearest existing ancestor so that
  // other URL segments are preserved
  assert.strictEqual(
    await resolveDestination(current, target, 'https://example.com/repo/main/en/b.html', probe),
    'https://example.com/repo/v1.0/en/')

  // Falls back to the target's home page when nothing exists
  assert.strictEqual(
    await resolveDestination(current, target, 'https://example.com/repo/main/en/b.html', () => Promise.resolve(false)),
    'https://example.com/repo/v1.0/')

  // Falls back to the target's home page when the current page is not under the current entry's URL
  assert.strictEqual(
    await resolveDestination(current, target, 'https://example.com/other/a.html', probe),
    'https://example.com/repo/v1.0/')

  // The query string and the fragment are not part of the destination
  assert.strictEqual(
    await resolveDestination(current, target, 'https://example.com/repo/main/a.html?q=1#x', probe),
    'https://example.com/repo/v1.0/a.html')

  // A target on another origin keeps its origin
  const mirror = { label: 'mirror', url: 'https://mirror.example.com/repo/' }
  const mirrorExists = new Set(['https://mirror.example.com/repo/en/'])
  assert.strictEqual(
    await resolveDestination(current, mirror, 'https://example.com/repo/main/en/b.html', url => Promise.resolve(mirrorExists.has(url))),
    'https://mirror.example.com/repo/en/')
})
