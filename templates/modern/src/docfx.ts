// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import 'bootstrap'
import { options } from './helper'
import { highlight } from './highlight'
import { renderMarkdown } from './markdown'
import { applyMonikerFilter } from './selector'
import { enableSearch } from './search'
import { renderToc } from './toc'
import { initTheme } from './theme'
import { renderBreadcrumb, renderInThisArticle, renderNavbar, renderOutline } from './nav'

import 'bootstrap-icons/font/bootstrap-icons.scss'
import './docfx.scss'

declare global {
  interface Window {
    docfx: {
      ready?: boolean,
      searchReady?: boolean,
      searchResultReady?: boolean,
    }
  }
}

async function init() {
  window.docfx = window.docfx || {}

  // The outline should reflect the content that is visible after moniker filtering
  document.addEventListener('docfx:monikerchanged', renderOutline)

  const { start } = await options()
  start?.()

  const pdfmode = navigator.userAgent.indexOf('docfx/pdf') >= 0
  if (pdfmode) {
    await Promise.all([
      renderMarkdown(),
      highlight()
    ])
  } else {
    let monikerSectionsHidden = false
    await Promise.all([
      initTheme(),
      enableSearch(),
      renderInThisArticle(),
      renderMarkdown(),
      renderNav(),
      applyMonikerFilter().then(hidden => {
        monikerSectionsHidden = hidden
      }),
      highlight()
    ])

    // The outline should reflect the content that is visible after moniker filtering
    if (monikerSectionsHidden) {
      renderOutline()
    }
  }

  window.docfx.ready = true

  async function renderNav() {
    const [navbar, toc] = await Promise.all([renderNavbar(), renderToc()])
    renderBreadcrumb([...navbar, ...toc])
  }
}

init().catch(console.error)
