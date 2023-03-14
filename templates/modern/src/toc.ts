// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { html, render } from 'lit-html'
import { classMap } from 'lit-html/directives/class-map.js'
import { meta } from './helper'

export type TocNode = {
  name: string
  href?: string
  expanded?: boolean
  items?: TocNode[]
}

/**
 * @returns active TOC nodes
 */
export async function renderToc(): Promise<TocNode[]> {
  const tocrel = meta('docfx:tocrel')
  if (!tocrel) {
    return []
  }

  const tocUrl = new URL(tocrel.replace(/.html$/gi, '.json'), window.location.href)
  const { items } = await (await fetch(tocUrl)).json()

  const activeNodes = []
  items.forEach(initTocNodes)

  const tocContainer = document.getElementById('toc')
  if (tocContainer) {
    renderToc()

    const activeElements = tocContainer.querySelectorAll('li.active')
    const lastActiveElement = activeElements[activeElements.length - 1]
    if (lastActiveElement) {
      lastActiveElement.scrollIntoView({ block: 'nearest' })
    }
  }

  return activeNodes.slice(0, -1)

  function initTocNodes(node: TocNode): boolean {
    let active
    if (node.href) {
      const url = new URL(node.href, tocUrl)
      node.href = url.href
      active = normalizeUrlPath(url) === normalizeUrlPath(window.location)
    }

    if (node.items) {
      for (const child of node.items) {
        if (initTocNodes(child)) {
          active = true
          node.expanded = true
        }
      }
    }

    if (active) {
      activeNodes.unshift(node)
      return true
    }
    return false
  }

  function renderToc() {
    render(renderTocNodes(items), tocContainer)
  }

  function renderTocNodes(nodes: TocNode[]) {
    return html`<ul>${nodes.map(node => {
      const { href, name, items, expanded } = node
      const isLeaf = !items || items.length <= 0

      return html`
        <li class=${classMap({ expanded })}>
          ${isLeaf ? null : html`<span class='expand-stub' @click=${toggleExpand}></span>`}
          ${href
            ? html`<a class='${classMap({ 'nav-link': !activeNodes.includes(node) })}' href=${href}>${name}</a>`
            : html`<a class='${classMap({ 'nav-link': !activeNodes.includes(node) })}' href='#' @click=${toggleExpand}>${name}</a>`}
          ${isLeaf ? null : html`<ul>${renderTocNodes(items)}</ul>`}
        </li>`

      function toggleExpand(e) {
        e.preventDefault()
        node.expanded = !node.expanded
        renderToc()
      }
    })}</ul>`
  }

  function normalizeUrlPath(url: { pathname: string }): string {
    return url.pathname.replace(/\/index\.html$/gi, '/')
  }
}
