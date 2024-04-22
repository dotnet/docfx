// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { html, TemplateResult } from 'lit-html'
import { DocfxOptions } from './options'

export async function options(): Promise<DocfxOptions> {
  return await import('./main.js').then(m => m.default) as DocfxOptions
}

/**
 * Get the value of an HTML meta tag.
 */
export function meta(name: string): string {
  return (document.querySelector(`meta[name="${name}"]`) as HTMLMetaElement)?.content
}

/**
 * Gets the localized text.
 * @param id key in token.json
 * @param args arguments to replace in the localized text
 */
export function loc(id: string, args?: { [key: string]: string }): string {
  let result = meta(`loc:${id}`) || id
  if (args) {
    for (const key in args) {
      result = result.replace(`{${key}}`, args[key])
    }
  }
  return result
}

/**
 * Add <wbr> into long word.
 */
export function breakWord(text?: string): string[] {
  if (!text) {
    return []
  }
  const regex = /([a-z0-9])([A-Z]+[a-z])|([a-zA-Z0-9][.,/<>_])/g
  const result = []
  let start = 0
  while (true) {
    const match = regex.exec(text)
    if (!match) {
      break
    }
    const index = match.index + (match[1] || match[3]).length
    result.push(text.slice(start, index))
    start = index
  }
  if (start < text.length) {
    result.push(text.slice(start))
  }
  return result
}

/**
 * Add <wbr> into long word.
 */
export function breakWordLit(text?: string): TemplateResult {
  const result = []
  breakWord(text).forEach(word => {
    if (result.length > 0) {
      result.push(html`<wbr>`)
    }
    result.push(html`${word}`)
  })
  return html`${result}`
}

/**
 * Check if the url is external.
 * @param url The url to check.
 * @returns True if the url is external.
 */
export function isExternalHref(url: URL): boolean {
  return url.hostname !== window.location.hostname || url.protocol !== window.location.protocol
}

/**
 * Determines if two URLs should be considered the same.
 */
export function isSameURL(a: { pathname: string }, b: { pathname: string }): boolean {
  return normalizeUrlPath(a) === normalizeUrlPath(b)

  function normalizeUrlPath(url: { pathname: string }): string {
    return url.pathname
      .replace(/\/index\.html$/gi, '/')
      .replace(/\.html$/gi, '')
      .replace(/\/$/gi, '')
      .toLowerCase()
  }
}
