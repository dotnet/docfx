// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import $ from 'jquery'

import '../node_modules/bootstrap/dist/css/bootstrap.css'
import '../node_modules/highlight.js/scss/github.scss'
import './docfx.scss'

import { highlight } from './highlight'
import { breakWord } from './helper'
import { renderMarkdown } from './markdown'
import { enableSearch } from './search'
import { renderToc } from './toc'
import { renderInThisArticle, renderNavbar } from './nav'

declare global {
  interface Window {
    $: object;
    jQuery: object;
    docfx: {
      ready?: boolean,
      searchResultReady?: boolean,
    };
  }
}

window.$ = window.jQuery = $
window.docfx = {}

require('bootstrap')
require('twbs-pagination')

document.addEventListener('DOMContentLoaded', function() {
  workAroundFixedHeaderForAnchors()
  highlight()
  enableSearch()

  renderMarkdown()
  renderNavbar()
  renderToc()
  renderInThisArticle()

  breakText()

  function breakText() {
    $('.xref').addClass('text-break')
    const texts = $('.text-break')
    texts.each(function() {
      breakWord($(this))
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

    window.docfx.ready = true
  }
})
