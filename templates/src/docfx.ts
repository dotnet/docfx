// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import $ from 'jquery'

import '../node_modules/bootstrap/dist/css/bootstrap.css'
import '../node_modules/highlight.js/scss/github.scss'
import './docfx.scss'

import { highlight } from './highlight'

import { breakWord, formList, getAbsolutePath, getCurrentWindowAbsolutePath, getDirectory, isRelativePath, meta } from './helper'
import { renderMarkdown } from './markdown'
import { enableSearch } from './search'

declare global {
  interface Window {
    $: object;
    jQuery: object;
    _docfxReady: boolean;
  }
}

window.$ = window.jQuery = $

require('bootstrap')
require('twbs-pagination')

document.addEventListener('DOMContentLoaded', function() {
  const active = 'active'
  const expanded = 'in'
  const filtered = 'filtered'
  const show = 'show'
  const hide = 'hide'

  workAroundFixedHeaderForAnchors()
  highlight()
  enableSearch()

  renderMarkdown()
  renderNavbar()
  renderSidebar()
  renderAffix()
  renderFooter()
  renderLogo()

  breakText()

  function breakText() {
    $('.xref').addClass('text-break')
    const texts = $('.text-break')
    texts.each(function() {
      breakWord($(this))
    })
  }

  // Update href in navbar
  function renderNavbar() {
    const navbar = $('#navbar ul')[0]
    if (typeof (navbar) === 'undefined') {
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
        $('#navbar').find('a[href]').each(function(i, e) {
          let href = $(e).attr('href')
          if (isRelativePath(href)) {
            href = navrel + href
            $(e).attr('href', href)

            let isActive = false
            let originalHref = e.name
            if (originalHref) {
              originalHref = navrel + originalHref
              if (getDirectory(getAbsolutePath(originalHref)) === getDirectory(getAbsolutePath(tocPath))) {
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

  function renderSidebar() {
    const sidetoc = $('#sidetoggle .sidetoc')[0]
    if (typeof (sidetoc) === 'undefined') {
      loadToc()
    } else {
      registerTocEvents()
      if ($('footer').is(':visible')) {
        $('.sidetoc').addClass('shiftup')
      }

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

      if ($('footer').is(':visible')) {
        $('.sidetoc').addClass('shiftup')
      }

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

        renderSidebar()
      })
    }
  }

  function renderBreadcrumb() {
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

  // Setup Affix
  function renderAffix() {
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
      const $headers = $($.map(['h1', 'h2', 'h3', 'h4'], function(h) { return '.article article ' + h }).join(', '))

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
        } else { // e.tagName[1] < frame.type[1]
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
        if (topLevel.length === 1) { // if there's only one topmost header, dump it
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

  // Show footer
  function renderFooter() {
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
      return (scrollHeight - scrollPosition) < 1
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

  function renderLogo() {
    // For LOGO SVG
    // Replace SVG with inline SVG
    // http://stackoverflow.com/questions/11978995/how-to-change-color-of-svg-image-using-css-jquery-svg-image-replacement
    $('img.svg').each(function() {
      const $img = $(this)
      const imgID = $img.attr('id')
      const imgClass = $img.attr('class')
      const imgURL = $img.attr('src')

      $.get(imgURL, function(data) {
        // Get the SVG tag, ignore the rest
        let $svg = $(data).find('svg')

        // Add replaced image's ID to the new SVG
        if (typeof imgID !== 'undefined') {
          $svg = $svg.attr('id', imgID)
        }
        // Add replaced image's classes to the new SVG
        if (typeof imgClass !== 'undefined') {
          $svg = $svg.attr('class', imgClass + ' replaced-svg')
        }

        // Remove any invalid XML tags as per http://validator.w3.org
        $svg = $svg.removeAttr('xmlns:a')

        // Replace image with new SVG
        $img.replaceWith($svg)
      }, 'xml')
    })
  }

  // adjusted from https://stackoverflow.com/a/13067009/1523776
  function workAroundFixedHeaderForAnchors() {
    const HISTORY_SUPPORT = !!(history && history.pushState)
    const ANCHOR_REGEX = /^#[^ ]+$/

    function getFixedOffset() {
      return $('header').first().height()
    }

    /**
     * If the provided href is an anchor which resolves to an element on the
     * page, scroll to it.
     * @param  {String} href
     * @return {Boolean} - Was the href an anchor.
     */
    function scrollIfAnchor(href, pushToHistory) {
      let rect, anchorOffset

      if (!ANCHOR_REGEX.test(href)) {
        return false
      }

      const match = document.getElementById(href.slice(1))

      if (match) {
        rect = match.getBoundingClientRect()
        anchorOffset = window.pageYOffset + rect.top - getFixedOffset()
        window.scrollTo(window.pageXOffset, anchorOffset)

        // Add the state to history as-per normal anchor links
        if (HISTORY_SUPPORT && pushToHistory) {
          history.pushState({}, document.title, location.pathname + href)
        }
      }

      return !!match
    }

    /**
     * Attempt to scroll to the current location's hash.
     */
    function scrollToCurrent() {
      scrollIfAnchor(window.location.hash, false)
    }

    /**
     * If the click event's target was an anchor, fix the scroll position.
     */
    function delegateAnchors(e) {
      const elem = e.target

      if (scrollIfAnchor(elem.getAttribute('href'), true)) {
        e.preventDefault()
      }
    }

    $(window).on('hashchange', scrollToCurrent)

    $(window).on('load', function() {
      // scroll to the anchor if present, offset by the header
      scrollToCurrent()
    })

    $(document).ready(function() {
      // Exclude tabbed content case
      $('a:not([data-tab])').click(function(e) { delegateAnchors(e) })
    })

    window._docfxReady = true
  }
})
