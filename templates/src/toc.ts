// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import $ from 'jquery'
import { html, render } from 'lit-html'
import { breakWord, getAbsolutePath, getCurrentWindowAbsolutePath, isRelativePath, meta } from './helper'

const active = 'active'
const expanded = 'in'

export function renderToc() {
  const sidetoc = document.getElementById('sidetoc')
  if (!sidetoc) {
    return
  }

  if (sidetoc.childElementCount === 0) {
    loadToc()
  } else {
    registerTocEvents()

    // Scroll to active item
    let top = 0
    $('#toc a.active').parents('li').each(function(i, e) {
      $(e).addClass(active).addClass(expanded)
      $(e).children('a').addClass(active)
    })
    $('#toc a.active').parents('li').each(function(i, e) {
      top += $(e).position().top
    })
    $('.sidetoc').scrollTop(top - 50)

    renderBreadcrumb()
  }

  function registerTocEvents() {
    $('.toc li > .expand-stub').click(function(e) {
      $(e.target).parent().toggleClass(expanded)
    })
    $('.toc li > .expand-stub + a:not([href])').click(function(e) {
      $(e.target).parent().toggleClass(expanded)
    })
  }

  function loadToc() {
    let tocPath = meta('docfx:tocrel')
    if (!tocPath) {
      return
    }
    tocPath = tocPath.replace(/\\/g, '/')
    $('#sidetoc').load(tocPath, function() {
      const index = tocPath.lastIndexOf('/')
      let tocrel = ''
      if (index > -1) {
        tocrel = tocPath.substr(0, index + 1)
      }
      let currentHref = getCurrentWindowAbsolutePath()
      if (!currentHref.endsWith('.html')) {
        currentHref += '.html'
      }
      $('#sidetoc').find('a[href]').each(function(i, e) {
        let href = $(e).attr('href')
        if (isRelativePath(href)) {
          href = tocrel + href
          $(e).attr('href', href)
        }

        if (getAbsolutePath(e.href) === currentHref) {
          $(e).addClass(active)
        }

        breakWord($(e))
      })

      renderToc()
    })
  }
}

export function renderBreadcrumb() {
  const breadcrumb = []
  $('#navbar a.active').each(function(i, e) {
    breadcrumb.push({
      href: e.href,
      name: e.innerText
    })
  })
  $('#toc a.active').each(function(i, e) {
    breadcrumb.push({
      href: e.href,
      name: e.innerText
    })
  })

  render(
    html`
    <ol class="breadcrumb">
      ${breadcrumb.map(i => html`<li class="breadcrumb-item"><a href="${i.href}">${i.name}</a></li>`)}
    </ol>`,
    document.getElementById('breadcrumb'))
}
