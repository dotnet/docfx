// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from 'jsx-dom'
import { meta } from './helper'
import { renderToc, TocNode } from './toc'

export type NavItem = {
  name: string
  href: URL
}

export async function renderNav() {
  renderInThisArticle()
  const [activeNavItems, activeTocItems] = await Promise.all([renderNavbar(), renderToc()])
  renderBreadcrumb([...activeNavItems, ...activeTocItems])
}

/**
 * @returns active navbar items
 */
export async function renderNavbar(): Promise<NavItem[]> {
  const navrel = meta('docfx:navrel')
  if (!navrel) {
    return []
  }

  const navUrl = new URL(navrel, window.location.href)
  const { items } = await fetch(navUrl).then(res => res.json())
  const navItems = items.map(a => ({ name: a.name, href: new URL(a.href, navUrl) }))
  if (navItems.length <= 0) {
    return []
  }

  const activeItem = findActiveItem(navItems)
  document.getElementById('navbar')?.appendChild(
    <ul class='nav level1 navbar-nav'>
      {navItems.map(item => {
        const current = (item === activeItem ? 'page' : false)
        const active = (item === activeItem ? 'active' : null)
        return <li class={active}><a href={item.href} class={active} aria-current={current}>{item.name}</a></li>
      })}
    </ul>)

  return activeItem ? [activeItem] : []
}

export function renderBreadcrumb(items: (NavItem | TocNode)[]) {
  document.getElementById('breadcrumb')?.appendChild(
    <ul class='level1 breadcrumb'>
      {items.map(i => <li><a href={i.href}>{i.name}</a></li>)}
    </ul>)
}

export function renderInThisArticle() {
  const h2s = document.querySelectorAll<HTMLHeadingElement>('article h2')
  if (h2s.length <= 0) {
    return
  }

  const affix = document.getElementById('affix')
  affix?.appendChild(<h5>In this article</h5>)
  affix?.appendChild(
    <ul class='nav bs-docs-sidenav'>{
      Array.from(h2s).map(h2 => <li><a href={`#${h2.id}`}>{h2.innerText}</a></li>)
    }</ul>)
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
