// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render, html } from 'lit-html'
import { meta } from './helper'
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
          return html`<li class='nav-item'><a class='nav-link ${active}' aria-current=${current} href=${item.href}>${item.name}</a></li>`
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
      return html`<a href='${github}' class='btn border-0'><i class='bi bi-github'></i></a>`
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
          ${breadcrumb.map(i => html`<li class="breadcrumb-item"><a href="${i.href}">${i.name}</a></li>`)}
        </ol>`,
      container)
  }
}

export function renderInThisArticle() {
  const h2s = document.querySelectorAll<HTMLHeadingElement>('article h2')
  if (h2s.length <= 0) {
    return
  }

  const dom = html`
    <h5 class="border-bottom">In this article</h5>
    <ul>${Array.from(h2s).map(h2 => html`<li><a class="link-secondary" href="#${h2.id}">${h2.innerText}</a></li>`)}</ul>`

  render(dom, document.getElementById('affix'))
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
