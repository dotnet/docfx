// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import $ from 'jquery'
import { breakWord, formList, getAbsolutePath, getCurrentWindowAbsolutePath, isRelativePath, meta } from './helper'

const active = 'active'
const expanded = 'in'
const filtered = 'filtered'
const show = 'show'
const hide = 'hide'

export function renderToc() {
  const sidetoc = $('#sidetoggle .sidetoc')[0]
  if (typeof (sidetoc) === 'undefined') {
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
    const tocFilterInput = $('#toc_filter_input')
    const tocFilterClearButton = $('#toc_filter_clear')

    $('.toc .nav > li > .expand-stub').click(function(e) {
      $(e.target).parent().toggleClass(expanded)
    })
    $('.toc .nav > li > .expand-stub + a:not([href])').click(function(e) {
      $(e.target).parent().toggleClass(expanded)
    })
    tocFilterInput.on('input', function() {
      const val = this.value
      // Save filter string to local session storage
      if (typeof (Storage) !== 'undefined') {
        sessionStorage.filterString = val
      }
      if (val === '') {
        // Clear 'filtered' class
        $('#toc li').removeClass(filtered).removeClass(hide)
        tocFilterClearButton.fadeOut()
        return
      }
      tocFilterClearButton.fadeIn()

      // set all parent nodes status
      $('#toc li>a').filter(function(i, e) {
        return $(e).siblings().length > 0
      }).each(function(i, anchor) {
        const parent = $(anchor).parent()
        parent.addClass(hide)
        parent.removeClass(show)
        parent.removeClass(filtered)
      })

      // Get leaf nodes
      $('#toc li>a').filter(function(i, e) {
        return $(e).siblings().length === 0
      }).each(function(_, anchor) {
        let text = $(anchor).attr('title')
        const parent = $(anchor).parent()
        const parentNodes = parent.parents('ul>li')
        for (let i = 0; i < parentNodes.length; i++) {
          const parentText = $(parentNodes[i]).children('a').attr('title')
          if (parentText) text = parentText + '.' + text
        }
        if (filterNavItem(text, val)) {
          parent.addClass(show)
          parent.removeClass(hide)
        } else {
          parent.addClass(hide)
          parent.removeClass(show)
        }
      })
      $('#toc li>a').filter(function(i, e) {
        return $(e).siblings().length > 0
      }).each(function(i, anchor) {
        const parent = $(anchor).parent()
        if (parent.find('li.show').length > 0) {
          parent.addClass(show)
          parent.addClass(filtered)
          parent.removeClass(hide)
        } else {
          parent.addClass(hide)
          parent.removeClass(show)
          parent.removeClass(filtered)
        }
      })

      function filterNavItem(name, text) {
        if (!text) return true
        if (name && name.toLowerCase().indexOf(text.toLowerCase()) > -1) return true
        return false
      }
    })

    // toc filter clear button
    tocFilterClearButton.hide()
    tocFilterClearButton.on('click', function() {
      tocFilterInput.val('')
      tocFilterInput.trigger('input')
      if (typeof (Storage) !== 'undefined') {
        sessionStorage.filterString = ''
      }
    })

    // Set toc filter from local session storage on page load
    if (typeof (Storage) !== 'undefined') {
      tocFilterInput.val(sessionStorage.filterString)
      tocFilterInput.trigger('input')
    }
  }

  function loadToc() {
    let tocPath = meta('docfx:tocrel')
    if (!tocPath) {
      return
    }
    tocPath = tocPath.replace(/\\/g, '/')
    $('#sidetoc').load(tocPath + ' #sidetoggle > div', function() {
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
      name: e.innerHTML
    })
  })
  $('#toc a.active').each(function(i, e) {
    breadcrumb.push({
      href: e.href,
      name: e.innerHTML
    })
  })

  const html = formList(breadcrumb, 'breadcrumb')
  $('#breadcrumb').html(html)
}
