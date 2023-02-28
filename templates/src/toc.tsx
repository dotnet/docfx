// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from 'jsx-dom'
import { meta } from './helper'

export type TocNode = {
  name: string
  href?: string
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

  const tocUrl = new URL(tocrel, window.location.href)
  const { items } = await (await fetch(tocUrl)).json()

  const activeNodes = []
  let activeElement: React.RefObject<HTMLElement>

  items.forEach(initTocNodes)
  document.getElementById('toc')?.appendChild(
    <ul class='nav level1'>{renderTocNodes(items, 2)}</ul>)
  registerTocEvents()

  if (activeElement) {
    activeElement.current.scrollIntoView({ block: 'nearest' })
  }

  return activeNodes

  function initTocNodes(node: TocNode): boolean {
    let active = false
    if (node.href) {
      const url = new URL(node.href, tocUrl)
      node.href = url.href
      if (url.pathname === window.location.pathname) {
        active = true
      }
    }

    if (node.items) {
      for (const child of node.items) {
        if (initTocNodes(child)) {
          active = true
        }
      }
    }

    if (active) {
      activeNodes.unshift(node)
      return true
    }
    return false
  }

  function renderTocNodes(nodes: TocNode[], level) {
    return nodes.map(node => {
      const li = React.createRef()
      const { href, name, items } = node
      const isLeaf = !items || items.length <= 0
      const active = activeNodes.includes(node)
      const activeClass = active ? 'active' : null

      if (active) {
        activeElement = li
      }

      return (
        <li ref={li} class={activeClass}>
          {isLeaf ? null : <i class='toggle' onClick={() => toggleTocNode(li.current)}></i>}
          {href
            ? <a class={activeClass} href={href}>{name}</a>
            : <a class={activeClass} onClick={() => toggleTocNode(li.current)}>{name}</a>}
          {isLeaf ? null : <ul class={['nav', `level${level}`]}>{renderTocNodes(items, level + 1)}</ul>}
        </li>)
    })
  }

  function toggleTocNode(li: HTMLLIElement) {
    if (li.classList.contains('active') || li.classList.contains('filtered')) {
      li.classList.remove('active')
      li.classList.remove('filtered')
    } else {
      li.classList.add('active')
    }
  }

  function registerTocEvents() {
    const tocFilter = document.getElementById('toc_filter_input') as HTMLInputElement
    if (!tocFilter) {
      return
    }

    tocFilter.addEventListener('input', () => onTocFilterTextChange())

    // Set toc filter from local session storage on page load
    const filterString = sessionStorage?.filterString
    if (filterString) {
      tocFilter.value = filterString
      onTocFilterTextChange()
    }

    function onTocFilterTextChange() {
      const filter = tocFilter.value?.toLocaleLowerCase() || ''
      if (sessionStorage) {
        sessionStorage.filterString = filter
      }

      const toc = document.getElementById('toc')
      const anchors = toc.querySelectorAll('a')

      if (filter === '') {
        anchors.forEach(a => a.parentElement.classList.remove('filtered', 'hide'))
        return
      }

      const filteredLI = new Set<HTMLElement>()
      anchors.forEach(a => {
        const text = a.innerText
        if (text && text.toLowerCase().indexOf(filter) >= 0) {
          let e: HTMLElement = a
          while (e && e !== toc) {
            e = e.parentElement
            filteredLI.add(e)
          }
        }
      })

      anchors.forEach(a => {
        const li = a.parentElement
        if (filteredLI.has(li)) {
          li.classList.remove('hide')
          li.classList.add('filtered')
        } else {
          li.classList.add('hide')
        }
      })
    }
  }
}
