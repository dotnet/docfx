// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import $ from 'jquery'

import '../node_modules/bootstrap/dist/css/bootstrap.css'
import '../node_modules/highlight.js/scss/github.scss'
import './docfx.scss'

import { highlight } from './highlight'
import { breakText } from './helper'
import { renderMarkdown } from './markdown'
import { enableSearch } from './search'
import { renderNav } from './nav'

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
  renderNav()
  breakText()

  window.docfx.ready = true
})
