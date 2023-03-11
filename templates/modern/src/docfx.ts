// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'bootstrap'
import { highlight } from './highlight'
import { breakText } from './helper'
import { renderMarkdown } from './markdown'
import { enableSearch } from './search'
import { renderToc } from './toc'
import { renderBreadcrumb, renderInThisArticle, renderNavbar } from './nav'

import 'bootstrap-icons/font/bootstrap-icons.scss'
import './docfx.scss'

declare global {
  interface Window {
    docfx: {
      ready?: boolean,
      searchReady?: boolean,
      searchResultReady?: boolean,
    };
  }
}

window.docfx = {}
document.addEventListener('DOMContentLoaded', function() {
  highlight()
  enableSearch()

  renderMarkdown()

  Promise.all([renderNavbar(), renderToc()])
    .then(([navbar, toc]) => renderBreadcrumb([...navbar, ...toc]))

  renderInThisArticle()

  breakText()

  window.docfx.ready = true
})
