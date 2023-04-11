// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'bootstrap'
import { highlight } from './highlight'
import { renderMarkdown } from './markdown'
import { enableSearch } from './search'
import { renderToc } from './toc'
import { initTheme } from './theme'
import { renderBreadcrumb, renderInThisArticle, renderNavbar } from './nav'

import 'bootstrap-icons/font/bootstrap-icons.scss'
import './docfx.scss'
import 'mathjax/es5/tex-svg-full.js'

declare global {
  interface Window {
    docfx: {
      ready?: boolean,
      searchReady?: boolean,
      searchResultReady?: boolean,
    }
  }
}

window.docfx = {}

initTheme()

document.addEventListener('DOMContentLoaded', function() {
  enableSearch()
  renderMarkdown()
  highlight()

  Promise.all([renderNavbar(), renderToc()])
    .then(([navbar, toc]) => renderBreadcrumb([...navbar, ...toc]))

  renderInThisArticle()
  window.docfx.ready = true
})
