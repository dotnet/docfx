// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import 'bootstrap'
import { options } from './helper'
import { highlight } from './highlight'
import { renderMarkdown } from './markdown'
import { enableSearch } from './search'
import { renderToc } from './toc'
import { initTheme } from './theme'
import { renderBreadcrumb, renderInThisArticle, renderNavbar } from './nav'

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

  const { start } = await options()
  start?.()

  const pdfmode = navigator.userAgent.indexOf('docfx/pdf') >= 0
  if (pdfmode) {
    await Promise.all([
      renderMarkdown(),
      highlight()
    ])
  } else {
    await Promise.all([
      initTheme(),
      enableSearch(),
      renderInThisArticle(),
      renderMarkdown(),
      renderNav(),
      highlight()
    ])
  }

  window.docfx.ready = true

  async function renderNav() {
    const [navbar, toc] = await Promise.all([renderNavbar(), renderToc()])
    renderBreadcrumb([...navbar, ...toc])
  }
}

init().catch(console.error)
