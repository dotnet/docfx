// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render, html, TemplateResult } from 'lit-html'
import { breakWordLit, meta } from './helper'
import { themePicker } from './theme'
import { TocNode } from './toc'

export type NavItem = {
  name: string
  href: URL
}

/**
 * @returns active navbar items
 */
export async function renderNavbar(): Promise<NavItem[]> {
  const navrel = meta('docfx:navrel')
  if (!navrel) {
    return []
  }

  const navUrl = new URL(navrel.replace(/.html$/gi, '.json'), window.location.href)
  const { items } = await fetch(navUrl).then(res => res.json())
  const navItems = items.map(a => ({ name: a.name, href: new URL(a.href, navUrl) }))
  if (navItems.length <= 0) {
    return []
  }

  const activeItem = findActiveItem(navItems)
  const navbar = document.getElementById('navbar')
  if (navbar) {
    const menu = html`
      <ul class='navbar-nav'>${
        navItems.map(item => {
          const current = (item === activeItem ? 'page' : false)
          const active = (item === activeItem ? 'active' : null)
          return html`
            <li class='nav-item'><a class='nav-link ${active}' aria-current=${current} href=${item.href}>${breakWordLit(item.name)}</a></li>`
        })
      }</ul>`

    render(menu, navbar, { renderBefore: navbar.firstChild })
  }

  return activeItem ? [activeItem] : []
}

export function renderFooter() {
  const footer = document.querySelector('footer>div') as HTMLElement
  if (footer) {
    render(html`${githubLink()} ${themePicker(renderFooter)}`, footer)
  }

  function githubLink() {
    const docurl = meta('docfx:docurl')
    const github = parseGitHubRepo(docurl)
    if (github) {
      return html`<a href='${github}' title='GitHub' class='btn border-0'><i class='bi bi-github'></i></a>`
    }
  }

  function parseGitHubRepo(url: string): string {
    const match = /^https:\/\/github.com\/([^/]+\/[^/]+)/gi.exec(url)
    if (match) {
      return match[0]
    }
  }
}

export function renderBreadcrumb(breadcrumb: (NavItem | TocNode)[]) {
  const container = document.getElementById('breadcrumb')
  if (container) {
    render(
      html`
        <ol class="breadcrumb">
          ${breadcrumb.map(i => html`<li class="breadcrumb-item"><a href="${i.href}">${breakWordLit(i.name)}</a></li>`)}
        </ol>`,
      container)
  }
}

export function renderInThisArticle() {
  const affix = document.getElementById('affix')
  if (affix) {
    render(meta('docfx:yamlmime') === 'ManagedReference' ? inThisArticleForManagedReference() : inThisArticleForConceptual(), affix)
  }
}

function inThisArticleForConceptual() {
  const headings = document.querySelectorAll<HTMLHeadingElement>('article h2')
  if (headings.length > 0) {
    return html`
      <h5 class="border-bottom">In this article</h5>
      <ul>${Array.from(headings).map(h => html`<li><a class="link-secondary" href="#${h.id}">${breakWordLit(h.innerText)}</a></li>`)}</ul>`
  }
}

function inThisArticleForManagedReference(): TemplateResult {
  let headings = Array.from(document.querySelectorAll<HTMLHeadingElement>('article h3, article h4'))
  headings = headings.filter((h, i) => h.tagName === 'H4' || headings[i + 1]?.tagName === 'H4')

  if (headings.length > 0) {
    return html`
      <h5 class="border-bottom">In this article</h5>
      <ul>${headings.map(h => {
        return h.tagName === 'H3'
          ? html`<li><h6>${breakWordLit(h.innerText)}</h6></li>`
          : html`<li><a class="link-secondary" href="#${h.id}">${breakWordLit(h.innerText)}</a></li>`
      })}</ul>`
  }
}

function findActiveItem(items: NavItem[]): NavItem {
  const url = new URL(window.location.href)
  let activeItem: NavItem
  let maxPrefix = 0
  for (const item of items) {
    const prefix = commonUrlPrefix(url, item.href)
    if (prefix > maxPrefix) {
      maxPrefix = prefix
      activeItem = item
    }
  }
  return activeItem
}

function commonUrlPrefix(url: URL, base: URL): number {
  const urlSegments = url.pathname.split('/')
  const baseSegments = base.pathname.split('/')
  let i = 0
  while (i < urlSegments.length && i < baseSegments.length && urlSegments[i] === baseSegments[i]) {
    i++
  }
  return i
}
