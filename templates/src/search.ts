// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { meta } from './helper'
import $ from 'jquery'
import Mark from 'mark.js'

const collapsed = 'collapsed'

/**
 * Support full-text-search
 */
export function enableSearch() {
  let query
  const relHref = meta('docfx:rel')
  if (typeof relHref === 'undefined') {
    return
  }
  try {
    webWorkerSearch()
    renderSearchBox()
    highlightKeywords()
    addSearchEvent()
  } catch (e) {
    console.error(e)
  }

  // Adjust the position of search box in navbar
  function renderSearchBox() {
    autoCollapse()
    $(window).on('resize', autoCollapse)
    $(document).on('click', '.navbar-collapse.in', function(e) {
      if ($(e.target).is('a')) {
        $(this).collapse('hide')
      }
    })

    function autoCollapse() {
      const navbar = $('#autocollapse')
      if (navbar.height() === null) {
        setTimeout(autoCollapse, 300)
      }
      navbar.removeClass(collapsed)
      if (navbar.height() > 60) {
        navbar.addClass(collapsed)
      }
    }
  }

  function webWorkerSearch() {
    const indexReady = $.Deferred()

    const worker = new Worker(relHref + 'styles/search-worker.min.js')
    worker.onmessage = function(oEvent) {
      switch (oEvent.data.e) {
        case 'index-ready':
          indexReady.resolve()
          break
        case 'query-ready':
          handleSearchResults(oEvent.data.d)
          break
      }
    }

    indexReady.promise().done(function() {
      $('body').bind('queryReady', function() {
        worker.postMessage({ q: query })
      })
      if (query && (query.length >= 3)) {
        worker.postMessage({ q: query })
      }
    })
  }

  // Highlight the searching keywords
  function highlightKeywords() {
    const q = new URLSearchParams(window.location.search).get('q')
    if (q) {
      const keywords = q.split('%20')
      keywords.forEach(function(keyword) {
        if (keyword !== '') {
          mark('.data-searchable *', keyword)
          mark('article *', keyword)
        }
      })
    }
  }

  function addSearchEvent() {
    $('body').bind('searchEvent', function() {
      $('#search-query').keypress(function(e) {
        return e.which !== 13
      })

      $('#search-query').keyup(function() {
        query = $(this).val()
        if (query.length < 3) {
          flipContents('show')
        } else {
          flipContents('hide')
          $('body').trigger('queryReady')
          $('#search-results>.search-list>span').text('"' + query + '"')
        }
      }).off('keydown')
    })
  }

  function flipContents(action) {
    if (action === 'show') {
      $('.hide-when-search').show()
      $('#search-results').hide()
    } else {
      $('.hide-when-search').hide()
      $('#search-results').show()
    }
  }

  function relativeUrlToAbsoluteUrl(currentUrl, relativeUrl) {
    const currentItems = currentUrl.split(/\/+/)
    const relativeItems = relativeUrl.split(/\/+/)
    let depth = currentItems.length - 1
    const items = []
    for (let i = 0; i < relativeItems.length; i++) {
      if (relativeItems[i] === '..') {
        depth--
      } else if (relativeItems[i] !== '.') {
        items.push(relativeItems[i])
      }
    }
    return currentItems.slice(0, depth).concat(items).join('/')
  }

  function extractContentBrief(content) {
    const briefOffset = 512
    const words = query.split(/\s+/g)
    const queryIndex = content.indexOf(words[0])
    if (queryIndex > briefOffset) {
      return '...' + content.slice(queryIndex - briefOffset, queryIndex + briefOffset) + '...'
    } else if (queryIndex <= briefOffset) {
      return content.slice(0, queryIndex + briefOffset) + '...'
    }
  }

  function handleSearchResults(hits) {
    const numPerPage = 10
    const pagination = $('#pagination')
    pagination.empty()
    pagination.removeData('twbs-pagination')
    if (hits.length === 0) {
      $('#search-results>.sr-items').html('<p>No results found</p>')
    } else {
      pagination.twbsPagination({
        first: pagination.data('first'),
        prev: pagination.data('prev'),
        next: pagination.data('next'),
        last: pagination.data('last'),
        totalPages: Math.ceil(hits.length / numPerPage),
        visiblePages: 5,
        onPageClick: function(event, page) {
          const start = (page - 1) * numPerPage
          const curHits = hits.slice(start, start + numPerPage)
          $('#search-results>.sr-items').empty().append(
            curHits.map(function(hit) {
              const currentUrl = window.location.href
              const itemRawHref = relativeUrlToAbsoluteUrl(currentUrl, relHref + hit.href)
              const itemHref = relHref + hit.href + '?q=' + query
              const itemTitle = hit.title
              const itemBrief = extractContentBrief(hit.keywords)

              const itemNode = $('<div>').attr('class', 'sr-item')
              const itemTitleNode = $('<div>').attr('class', 'item-title').append($('<a>').attr('href', itemHref).attr('target', '_blank').attr('rel', 'noopener noreferrer').text(itemTitle))
              const itemHrefNode = $('<div>').attr('class', 'item-href').text(itemRawHref)
              const itemBriefNode = $('<div>').attr('class', 'item-brief').text(itemBrief)
              itemNode.append(itemTitleNode).append(itemHrefNode).append(itemBriefNode)
              return itemNode
            })
          )
          query.split(/\s+/).forEach(function(word) {
            if (word !== '') {
              mark('#search-results>.sr-items *', word)
            }
          })
        }
      })
    }

    window.docfx.searchResultReady = true
  }
}

function mark(selector: string, keyword: string) {
  new Mark(document.querySelectorAll(selector)).mark(keyword)
}
