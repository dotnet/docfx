// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import $ from 'jquery'
import {
  formList,
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

export function renderAffix() {
  const hierarchy = getHierarchy()
  if (!hierarchy || hierarchy.length <= 0) {
    $('#affix').hide()
  } else {
    const html = formList(hierarchy, ['nav', 'bs-docs-sidenav'])
    $('#affix>div').empty().append(html)
    if ($('footer').is(':visible')) {
      $('.sideaffix').css('bottom', '70px')
    }
    $('#affix a').click(function(e) {
      const scrollspy = $('[data-spy="scroll"]').data()['bs.scrollspy']
      const target = e.target.hash
      if (scrollspy && target) {
        scrollspy.activate(target)
      }
    })
  }

  function getHierarchy() {
    // supported headers are h1, h2, h3, and h4
    const $headers = $(
      $.map(['h1', 'h2', 'h3', 'h4'], function(h) {
        return '.article article ' + h
      }).join(', ')
    )

    // a stack of hierarchy items that are currently being built
    const stack = []
    $headers.each(function(i, e) {
      if (!e.id) {
        return
      }

      const item = {
        name: htmlEncode($(e).text()),
        href: '#' + e.id,
        items: []
      }

      if (!stack.length) {
        stack.push({ type: e.tagName, siblings: [item] })
        return
      }

      const frame = stack[stack.length - 1]
      if (e.tagName === frame.type) {
        frame.siblings.push(item)
      } else if (e.tagName[1] > frame.type[1]) {
        // we are looking at a child of the last element of frame.siblings.
        // push a frame onto the stack. After we've finished building this item's children,
        // we'll attach it as a child of the last element
        stack.push({ type: e.tagName, siblings: [item] })
      } else {
        // e.tagName[1] < frame.type[1]
        // we are looking at a sibling of an ancestor of the current item.
        // pop frames from the stack, building items as we go, until we reach the correct level at which to attach this item.
        while (e.tagName[1] < stack[stack.length - 1].type[1]) {
          buildParent()
        }
        if (e.tagName === stack[stack.length - 1].type) {
          stack[stack.length - 1].siblings.push(item)
        } else {
          stack.push({ type: e.tagName, siblings: [item] })
        }
      }
    })
    while (stack.length > 1) {
      buildParent()
    }

    function buildParent() {
      const childrenToAttach = stack.pop()
      const parentFrame = stack[stack.length - 1]
      const parent = parentFrame.siblings[parentFrame.siblings.length - 1]
      $.each(childrenToAttach.siblings, function(i, child) {
        parent.items.push(child)
      })
    }
    if (stack.length > 0) {
      const topLevel = stack.pop().siblings
      if (topLevel.length === 1) {
        // if there's only one topmost header, dump it
        return topLevel[0].items
      }
      return topLevel
    }
    return undefined
  }

  function htmlEncode(str) {
    if (!str) return str
    return str
      .replace(/&/g, '&amp;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
  }
}

export function renderFooter() {
  initFooter()
  $(window).on('scroll', showFooterCore)

  function initFooter() {
    if (needFooter()) {
      shiftUpBottomCss()
      $('footer').show()
    } else {
      resetBottomCss()
      $('footer').hide()
    }
  }

  function showFooterCore() {
    if (needFooter()) {
      shiftUpBottomCss()
      $('footer').fadeIn()
    } else {
      resetBottomCss()
      $('footer').fadeOut()
    }
  }

  function needFooter() {
    const scrollHeight = $(document).height()
    const scrollPosition = $(window).height() + $(window).scrollTop()
    return scrollHeight - scrollPosition < 1
  }

  function resetBottomCss() {
    $('.sidetoc').removeClass('shiftup')
    $('.sideaffix').removeClass('shiftup')
  }

  function shiftUpBottomCss() {
    $('.sidetoc').addClass('shiftup')
    $('.sideaffix').addClass('shiftup')
  }
}
