// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { html, TemplateResult } from 'lit-html'
import { loc, options } from './helper'
import { LocaleOption, VersionOption } from './options'

export type SiteEntry = {
  /** The display label of this version or locale. */
  label: string,
  /** The base URL of this version or locale site. */
  url: string,
  /** The monikers this version covers, e.g. `net-5.0 net-7.0`. */
  moniker?: string,
}

const manifestCache = new Map<string, Promise<SiteEntry[]>>()

/**
 * Ensures a base URL ends with `/`, so `/repo/v2.0` matches pages under `/repo/v2.0/`
 * but not pages under `/repo/v2.0.1/`.
 */
export function normalizeUrl(url: string): string {
  return url.replace(/\/?$/, '/')
}

/**
 * Parses version or locale entries, either from a `versions.json` / `locales.json`
 * manifest or from the template options. Entry URLs are resolved against `baseUrl`.
 */
export function parseEntries(content: unknown, baseUrl: string): SiteEntry[] {
  if (!Array.isArray(content)) {
    return []
  }

  const entries: SiteEntry[] = []
  for (const item of content) {
    if (typeof item?.label === 'string' && item.label !== '' && typeof item?.url === 'string' && item.url !== '') {
      const entry: SiteEntry = { label: item.label, url: normalizeUrl(new URL(item.url, baseUrl).href) }
      if (typeof item?.moniker === 'string' && item.moniker !== '') {
        entry.moniker = item.moniker
      }
      entries.push(entry)
    }
  }
  return entries
}

/**
 * Finds the version or locale of the current page: the entry whose URL is the longest
 * prefix of the page URL. This supports sites that serve one version from the site root
 * and the other versions from subfolders, e.g. `v2.0 (stable)` at `/repo/` and
 * `v3.0` at `/repo/v3.0/`.
 */
export function findCurrentEntry(entries: SiteEntry[], href: string): SiteEntry {
  let current: SiteEntry
  for (const entry of entries) {
    if (href.toLowerCase().startsWith(entry.url.toLowerCase()) && (!current || current.url.length <= entry.url.length)) {
      current = entry
    }
  }
  return current
}

/**
 * Removes entries with a duplicate label, keeping the position of the first occurrence.
 */
export function dedupeEntries(entries: SiteEntry[]): SiteEntry[] {
  const seen = new Set<string>()
  const result: SiteEntry[] = []
  for (const entry of entries) {
    if (!seen.has(entry.label)) {
      seen.add(entry.label)
      result.push(entry)
    }
  }
  return result
}

/**
 * Finds the entry of a set of entries with the same label that shares the longest URL
 * prefix with the current page URL. For example with entries `/repo/main/en/` and
 * `/repo/v2.0/en/`, switching the locale of the page `/repo/v2.0/ja/x.html` selects
 * the `/repo/v2.0/en/` entry so that the version segment of the URL is preserved.
 */
export function findTargetEntry(candidates: SiteEntry[], href: string): SiteEntry {
  let target = candidates[0]
  for (const candidate of candidates) {
    if (commonUrlPrefixLength(candidate.url, href) > commonUrlPrefixLength(target.url, href)) {
      target = candidate
    }
  }
  return target
}

function commonUrlPrefixLength(a: string, b: string): number {
  const as = a.toLowerCase().split('/')
  const bs = b.toLowerCase().split('/')
  let length = 0
  while (length < as.length && length < bs.length && as[length] === bs[length]) {
    length++
  }
  return length
}

/**
 * Extracts moniker names from a moniker range or the `data-moniker` attribute of a
 * moniker section, e.g. `net-5.0 || net-7.0` or `net-5.0 net-7.0`.
 */
export function parseMonikers(monikers: string): string[] {
  return monikers.split(/[|\s]+/).map(m => m.trim()).filter(m => m !== '')
}

/**
 * Determines whether a moniker section applies to a version: the section and the version
 * overlap in at least one moniker. Sections without monikers always apply, and versions
 * without monikers keep every section visible.
 */
export function monikerSectionApplies(sectionMonikers: string, versionMonikers: string): boolean {
  const section = parseMonikers(sectionMonikers)
  const version = parseMonikers(versionMonikers)
  return section.length === 0 || version.length === 0 || version.some(m => section.includes(m))
}

/**
 * Extracts moniker names suitable as moniker selector entries from monikers, dropping
 * range operators such as `>=` and `&&`.
 */
export function parseMonikerNames(monikers: string): string[] {
  return [...new Set(parseMonikers(monikers).filter(m => /^[A-Za-z0-9._-]+$/.test(m)))]
}

/**
 * Resolves the moniker range that applies to the current page: the range picked by the
 * reader when it covers monikers of the page, falling back to the range declared by the
 * current version — which applies even when it covers no moniker of the page, so that
 * sections of other versions stay hidden —, and to no filtering when neither applies.
 */
export function resolveSelectedMoniker(monikers: string[], stored: string | null, declared: string | null): string | null {
  if (stored && parseMonikers(stored).some(m => monikers.includes(m))) {
    return stored
  }
  return declared || null
}

/**
 * Resolves the URL of the same page in another version or locale site, returns
 * `null` when the current page is not under the current entry's URL.
 */
export function resolveEntryUrl(current: SiteEntry, target: SiteEntry, href: string): string {
  if (!current || !href.toLowerCase().startsWith(current.url.toLowerCase())) {
    return null
  }
  return target.url + href.slice(current.url.length)
}

/**
 * Loads version or locale entries from the template options. The option is either
 * the URL of a manifest file, or the entries inline.
 */
async function loadEntries(option: string | (VersionOption | LocaleOption)[] | undefined): Promise<SiteEntry[]> {
  if (typeof option === 'string') {
    return fetchManifest(new URL(option, window.location.href).href)
  }
  if (Array.isArray(option)) {
    return parseEntries(option, window.location.href)
  }
  return []
}

function fetchManifest(url: string): Promise<SiteEntry[]> {
  let manifest = manifestCache.get(url)
  if (!manifest) {
    manifest = fetch(url)
      .then(res => res.json())
      .then(content => parseEntries(content, url), () => [])
    manifestCache.set(url, manifest)
  }
  return manifest
}

const monikerStorageKey = 'moniker'

/**
 * Gets the monikers of the current page from moniker sections (`div[data-moniker]`).
 */
function pageMonikers(): string[] {
  const result = new Set<string>()
  document.querySelectorAll<HTMLElement>('article div[data-moniker]').forEach(el => {
    parseMonikers(el.dataset.moniker ?? '').forEach(m => result.add(m))
  })
  return [...result]
}

/**
 * Gets the moniker declared by the version of the current page, when the versions
 * option is configured and the current version entry declares monikers.
 */
async function currentVersionMoniker(): Promise<string | null> {
  const { versions } = await options()
  if (!versions) {
    return null
  }
  return findCurrentEntry(await loadEntries(versions), window.location.href)?.moniker ?? null
}

/**
 * Shows moniker sections that apply to the selected moniker range, hides the others, and
 * returns whether the visibility of any section changed. Sections without monikers are
 * always shown.
 */
function filterPageMonikers(selected: string | null): boolean {
  let changed = false
  document.querySelectorAll<HTMLElement>('article div[data-moniker]').forEach(el => {
    const applies = selected === null || monikerSectionApplies(el.dataset.moniker ?? '', selected)
    const display = applies ? '' : 'none'
    if (el.style.display !== display) {
      el.style.display = display
      changed = true
    }
  })
  return changed
}

/**
 * Applies a moniker selection to the page: filters the moniker sections, updates the
 * active item of the moniker selector and refreshes the outline when the visible content
 * changed. This is the single state-update path shared by the initial page load and the
 * moniker selector.
 */
function applyMonikerSelection(selected: string | null): boolean {
  const changed = filterPageMonikers(selected)
  if (changed) {
    // The outline should reflect the content that is visible after moniker filtering
    document.dispatchEvent(new CustomEvent('docfx:monikerchanged'))
  }
  document.querySelectorAll('.moniker-menu .dropdown-item').forEach(item => {
    item.classList.toggle('active', item.textContent?.trim() === selected)
  })
  return changed
}

/**
 * Renders the version selector in the navbar when more than one version is available.
 */
export async function versionSelector(): Promise<TemplateResult | string> {
  const { versions } = await options()
  return siteSelector('changeVersion', 'tag', loadEntries(versions))
}

/**
 * Renders the locale selector in the navbar when more than one locale is available.
 */
export async function localeSelector(): Promise<TemplateResult | string> {
  const { locales } = await options()
  return siteSelector('changeLocale', 'translate', loadEntries(locales))
}

/**
 * Renders the moniker selector in the navbar when the current page contains more than
 * one moniker. Selecting a moniker hides the moniker sections that don't apply to it,
 * and the selection is restored when browsing to other pages.
 */
export async function monikerSelector(): Promise<TemplateResult | string> {
  const monikers = pageMonikers()
  const names = parseMonikerNames(monikers.join(' '))

  if (names.length <= 1) {
    return ''
  }

  const stored = localStorage.getItem(monikerStorageKey)
  const declared = await currentVersionMoniker()
  const effective = resolveSelectedMoniker(monikers, stored, declared)

  return html`
    <div class='dropdown'>
      <a title='${loc('changeMoniker')}' aria-label='${loc('changeMoniker')}' class='btn border-0 dropdown-toggle' data-bs-toggle='dropdown' aria-expanded='false'>
        <i class='bi bi-layers'></i>
      </a>
      <ul class='dropdown-menu dropdown-menu-end moniker-menu'>
        ${names.map(name => html`
          <li><a class='dropdown-item ${name === effective ? 'active' : null}' href='#' @click=${ev => changeMoniker(ev, name)}>${name}</a></li>`)}
      </ul>
    </div>`

  function changeMoniker(ev, moniker: string) {
    ev.preventDefault()

    // The selection may have changed since the dropdown was rendered,
    // re-derive the moniker that currently applies to the page
    const current = resolveSelectedMoniker(monikers, localStorage.getItem(monikerStorageKey), declared)
    if (moniker === current) {
      return
    }

    localStorage.setItem(monikerStorageKey, moniker)
    applyMonikerSelection(moniker)
  }
}

/**
 * Hides the moniker sections that don't apply to the selected moniker, and returns
 * whether the visibility of any section changed. The selected moniker is the one picked
 * in the moniker selector, falling back to the moniker declared by the current version.
 * Sections without monikers are always shown, and pages without moniker sections keep
 * all their content. Runs on every page, even when the navbar (and thus the selectors)
 * is disabled, and is skipped when generating PDF.
 */
export async function applyMonikerFilter(): Promise<boolean> {
  if (navigator.userAgent.indexOf('docfx/pdf') >= 0) {
    return false
  }

  const monikers = pageMonikers()
  if (monikers.length === 0) {
    return false
  }

  const declared = await currentVersionMoniker()
  const selected = resolveSelectedMoniker(monikers, localStorage.getItem(monikerStorageKey), declared)
  return applyMonikerSelection(selected)
}

async function siteSelector(locId: string, icon: string, entries: Promise<SiteEntry[]>): Promise<TemplateResult | string> {
  const items = await entries
  const current = findCurrentEntry(items, window.location.href)

  // Entries with the same label are one locale or version hosted under
  // multiple URLs, e.g. one entry per version in a combined version and
  // locale site. They are displayed once in the dropdown.
  const displayEntries = dedupeEntries(items)

  if (displayEntries.length <= 1) {
    return ''
  }

  const candidatesByLabel = new Map<string, SiteEntry[]>()
  for (const item of items) {
    candidatesByLabel.set(item.label, [...(candidatesByLabel.get(item.label) ?? []), item])
  }

  return html`
    <div class='dropdown'>
      <a title='${loc(locId)}' aria-label='${loc(locId)}' class='btn border-0 dropdown-toggle' data-bs-toggle='dropdown' aria-expanded='false'>
        <i class='bi bi-${icon}'></i>
      </a>
      <ul class='dropdown-menu dropdown-menu-end'>
        ${displayEntries.map(item => html`
          <li><a class='dropdown-item ${current?.label === item.label ? 'active' : null}' href='${item.url}' @click=${ev => changeEntry(ev, item)}>${item.label}</a></li>`)}
      </ul>
    </div>`

  async function changeEntry(ev, target: SiteEntry) {
    ev.preventDefault()
    if (target.label === current?.label) {
      return
    }

    // Navigate to the same page in the selected version or locale, falling back to the
    // nearest existing page when the same page is missing. The query string and the
    // fragment of the current page are preserved.
    const entry = findTargetEntry(candidatesByLabel.get(target.label) ?? [target], window.location.href)
    const destination = await resolveDestination(current, entry, window.location.href)
    window.location.href = new URL(destination + window.location.search + window.location.hash, window.location.href).href
  }
}

async function pageExists(url: string): Promise<boolean> {
  return fetch(url.split(/[?#]/)[0], { method: 'HEAD' }).then(res => res.ok, () => false)
}

/**
 * Resolves the destination to navigate to when switching from the page `href` to the
 * `target` entry: the same page under the target's URL when it exists, otherwise the
 * nearest existing ancestor directory — so that other URL segments such as the locale
 * are preserved —, falling back to the target's home page. The destination is an
 * absolute URL on the target's origin, without query string or fragment.
 */
export async function resolveDestination(current: SiteEntry, target: SiteEntry, href: string, probe: (url: string) => Promise<boolean> = pageExists): Promise<string> {
  const home = normalizeUrl(new URL(target.url, href).href)
  const samePage = resolveEntryUrl(current, target, href)?.split(/[?#]/)[0]
  if (!samePage) {
    return home
  }
  if (await probe(samePage)) {
    return samePage
  }
  return walkUpToExisting(home, samePage, probe)
}

/**
 * Walks up from `url` towards `base`, returning the first existing ancestor directory of
 * `url`, or `base` when none of them exist.
 */
export async function walkUpToExisting(base: string, url: string, probe: (url: string) => Promise<boolean>): Promise<string> {
  let candidate = url.replace(/[^/]*$/, '')
  while (candidate.length > base.length && candidate.toLowerCase().startsWith(base.toLowerCase())) {
    if (await probe(candidate)) {
      return candidate
    }
    const parent = candidate.replace(/[^/]+\/$/, '')
    if (parent.length < base.length || parent === candidate) {
      break
    }
    candidate = parent
  }
  return base || url
}
