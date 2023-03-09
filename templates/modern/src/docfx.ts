// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import $ from 'jquery'

import 'bootstrap/dist/css/bootstrap.css'
import 'highlight.js/scss/github.scss'
import './docfx.scss'

import { highlight } from './highlight'
import { breakText } from './helper'
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
  highlight()
  enableSearch()

  renderMarkdown()
  renderNavbar()
  renderToc()
  renderInThisArticle()

  breakText()

  window.docfx.ready = true
})
