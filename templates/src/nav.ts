// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import $ from 'jquery'
import { render, html } from 'lit-html'
import {
  getAbsolutePath,
  getCurrentWindowAbsolutePath,
  getDirectory,
  isRelativePath,
  meta
} from './helper'
import { renderBreadcrumb } from './toc'

const active = 'active'

export function renderNavbar() {
  const navbar = $('#navbar ul')[0]
  if (typeof navbar === 'undefined') {
    loadNavbar()
  } else {
    $('#navbar ul a.active').parents('li').addClass(active)
    renderBreadcrumb()
    showSearch()
  }

  function showSearch() {
    if ($('#search-results').length !== 0) {
      $('#search').show()
      $('body').trigger('searchEvent')
    }
  }

  function loadNavbar() {
    let navbarPath = meta('docfx:navrel')
    if (!navbarPath) {
      return
    }
    navbarPath = navbarPath.replace(/\\/g, '/')
    let tocPath = meta('docfx:tocrel') || ''
    if (tocPath) tocPath = tocPath.replace(/\\/g, '/')
    $.get(navbarPath, function(data) {
      $(data).find('#toc>ul').appendTo('#navbar')
      showSearch()
      const index = navbarPath.lastIndexOf('/')
      let navrel = ''
      if (index > -1) {
        navrel = navbarPath.substr(0, index + 1)
      }
      $('#navbar>ul').addClass('navbar-nav')
      const currentAbsPath = getCurrentWindowAbsolutePath()
      // set active item
      $('#navbar')
        .find('a[href]')
        .each(function(i, e) {
          let href = $(e).attr('href')
          if (isRelativePath(href)) {
            href = navrel + href
            $(e).attr('href', href)

            let isActive = false
            let originalHref = e.name
            if (originalHref) {
              originalHref = navrel + originalHref
              if (
                getDirectory(getAbsolutePath(originalHref)) ===
                getDirectory(getAbsolutePath(tocPath))
              ) {
                isActive = true
              }
            } else {
              if (getAbsolutePath(href) === currentAbsPath) {
                const dropdown = $(e).attr('data-toggle') === 'dropdown'
                if (!dropdown) {
                  isActive = true
                }
              }
            }
            if (isActive) {
              $(e).addClass(active)
            }
          }
        })
      renderNavbar()
    })
  }
}

export function renderInThisArticle() {
  const h2s = document.querySelectorAll<HTMLHeadingElement>('article h2')
  if (h2s.length <= 0) {
    return
  }

  const dom = html`
    <h5>In this article</h5>
    <ul class="nav bs-docs-sidenav">${Array.from(h2s).map(h2 => html`<li><a href="#${h2.id}">${h2.innerText}</a></li>`)}</ul>`

  render(dom, document.getElementById('affix'))
}
