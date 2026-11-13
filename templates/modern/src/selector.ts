// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { html, TemplateResult } from 'lit-html'
import { loc, options } from './helper'
import { LocaleOption, VersionOption } from './options'

type SiteEntry = {
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

/**
 * Renders the version selector in the navbar when more than one version is available.
 * Moniker sections that don't apply to the current version are hidden.
 */
export async function versionSelector(): Promise<TemplateResult | string> {
  const { versions } = await options()
  return siteSelector('changeVersion', 'tag', loadEntries(versions), true)
}

/**
 * Renders the locale selector in the navbar when more than one locale is available.
 */
export async function localeSelector(): Promise<TemplateResult | string> {
  const { locales } = await options()
  return siteSelector('changeLocale', 'translate', loadEntries(locales))
}

async function siteSelector(locId: string, icon: string, entries: Promise<SiteEntry[]>, filterMonikers = false): Promise<TemplateResult | string> {
  const items = await entries
  const current = findCurrentEntry(items, window.location.href)

  // Hide moniker sections that don't apply to the current version
  if (filterMonikers && current?.moniker && navigator.userAgent.indexOf('docfx/pdf') < 0) {
    document.querySelectorAll<HTMLElement>('article div[data-moniker]').forEach(el => {
      el.style.display = monikerSectionApplies(el.dataset.moniker ?? '', current.moniker!) ? '' : 'none'
    })
  }

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

    // Navigate to the same page in the selected version or locale when it exists,
    // and to its home page otherwise
    const entry = findTargetEntry(candidatesByLabel.get(target.label) ?? [target], window.location.href)
    const url = resolveEntryUrl(current, entry, window.location.href) ?? entry.url
    const exists = await fetch(url.split('#')[0], { method: 'HEAD' }).then(res => res.ok, () => undefined)
    window.location.href = exists === false ? entry.url : url
  }
}
