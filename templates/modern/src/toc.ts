// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { TemplateResult, html, render } from 'lit-html'
import { classMap } from 'lit-html/directives/class-map.js'
import { breakWordLit, meta, isExternalHref, loc, isSameURL } from './helper'

export type TocNode = {
  name?: string
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

  const disableTocFilter = meta('docfx:disabletocfilter') === 'true'

  const tocUrl = new URL(tocrel.replace(/.html$/gi, '.json'), window.location.href)
  const { items, pdf, pdfFileName } = await (await fetch(tocUrl)).json()

  const tocFilterUrl = disableTocFilter ? '' : (localStorage?.getItem('tocFilterUrl') || '')
  let tocFilter = disableTocFilter ? '' : (localStorage?.getItem('tocFilter') || '')

  if (tocFilterUrl !== tocUrl.toString()) {
    tocFilter = ''
    localStorage?.setItem('tocFilterUrl', '')
  } else {
    localStorage?.setItem('tocFilterUrl', tocUrl.toString())
  }

  const activeNodes = []
  const selectedNodes = []
  items.forEach(initTocNodes)

  const tocContainer = document.getElementById('toc')
  if (tocContainer) {
    renderToc()

    const activeElements = tocContainer.querySelectorAll('li.active')
    const lastActiveElement = activeElements[activeElements.length - 1]
    if (lastActiveElement) {
      lastActiveElement.scrollIntoView({ block: 'nearest' })
      /* eslint-disable no-self-assign */
      if (location.hash) {
        location.href = location.href
      }
    }
  }

  if (selectedNodes.length > 0) {
    renderNextArticle(items, selectedNodes[0])
  }

  return activeNodes.slice(0, -1)

  function initTocNodes(node: TocNode): boolean {
    let active
    if (node.href) {
      const url = new URL(node.href, tocUrl)
      node.href = url.href
      active = isExternalHref(url) ? false : isSameURL(url, window.location)
      if (active) {
        if (node.items) {
          node.expanded = true
        }
        selectedNodes.push(node)
      }
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
    render(html`
      ${renderTocFilter()} 
      <div class="flex-fill overflow-y-auto">${renderTocNodes(items) || renderNoFilterResult()}</div>
      ${renderDownloadPdf()}`, tocContainer)
  }

  function renderTocNodes(nodes: TocNode[]): TemplateResult {
    const result = nodes.map(node => {
      const { href, name, items, expanded } = node
      if (!name) {
        return null
      }

      const isLeaf = !items || items.length <= 0

      const children = isLeaf ? null : renderTocNodes(items)
      if (tocFilter !== '' && !children && !name.toLowerCase().includes(tocFilter.toLowerCase())) {
        return null
      }

      const dom = href
        ? html`<a class='${classMap({ 'nav-link': !activeNodes.includes(node) })}' href=${href}>${breakWordLit(name)}</a>`
        : (isLeaf
            ? html`<span class='text-body-tertiary name-only'>${breakWordLit(name)}</a>`
            : html`<a class='${classMap({ 'nav-link': !activeNodes.includes(node) })}' href='#' @click=${toggleExpand}>${breakWordLit(name)}</a>`)

      const isExpanded = (tocFilter !== '' && expanded !== false && children != null) || expanded === true

      return html`
        <li class=${classMap({ expander: !isLeaf, expanded: isExpanded, active: activeNodes.includes(node) })}>
          ${isLeaf ? null : html`<span class='expand-stub' @click=${toggleExpand}></span>`}
          ${dom}
          ${children}
        </li>`

      function toggleExpand(e) {
        e.preventDefault()
        node.expanded = !isExpanded
        renderToc()
      }
    }).filter(node => node)

    return result.length > 0 ? html`<ul>${result}</ul>` : null
  }

  function renderTocFilter(): TemplateResult {
    return disableTocFilter
      ? null
      : html`
      <form class='filter'>
        <i class='bi bi-filter'></i>
        <input class='form-control' @input=${filterToc} value='${tocFilter}' type='search' placeholder='${loc('tocFilter')}' autocomplete='off' aria-label='${loc('tocFilter')}'>
      </form>`

    function filterToc(e: Event) {
      tocFilter = (<HTMLInputElement>e.target).value.trim()
      localStorage?.setItem('tocFilter', tocFilter)
      renderToc()
    }
  }

  function renderNoFilterResult(): TemplateResult {
    return tocFilter === '' ? null : html`<div class='no-result'>${loc('searchNoResults', { query: tocFilter })}</div>`
  }

  function renderDownloadPdf(): TemplateResult {
    return pdf ? html`<div class="py-2 mb-md-4"><a class="pdf-link" href="${new URL(pdfFileName || 'toc.pdf', tocUrl)}">${loc('downloadPdf')}</a></div>` : null
  }
}

function renderNextArticle(items: TocNode[], node: TocNode) {
  const nextArticle = document.getElementById('nextArticle')
  if (!nextArticle) {
    return
  }

  const tocNodes = flattenTocNodesWithHref(items)
  const i = tocNodes.findIndex(n => n === node)
  const prev = tocNodes[i - 1]
  const next = tocNodes[i + 1]
  if (!prev && !next) {
    return
  }

  const prevButton = prev ? html`<div class="prev"><span><i class='bi bi-chevron-left'></i> ${loc('prevArticle')}</span> <a href="${prev.href}" rel="prev">${breakWordLit(prev.name)}</a></div>` : null
  const nextButton = next ? html`<div class="next"><span>${loc('nextArticle')} <i class='bi bi-chevron-right'></i></span> <a href="${next.href}" rel="next">${breakWordLit(next.name)}</a></div>` : null

  render(html`${prevButton} ${nextButton}`, nextArticle)

  function flattenTocNodesWithHref(items: TocNode[]) {
    const result = []
    for (const item of items) {
      if (item.href) {
        result.push(item)
      }
      if (item.items) {
        result.push(...flattenTocNodesWithHref(item.items))
      }
    }
    return result
  }
}
