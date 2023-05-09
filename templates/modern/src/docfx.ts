// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'bootstrap'
import { DocfxOptions } from './options'
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
    docfx: DocfxOptions & {
      ready?: boolean,
      searchReady?: boolean,
      searchResultReady?: boolean,
    }
  }
}

export async function init(options: DocfxOptions) {
  window.docfx = Object.assign({}, options)

  initTheme()
  enableSearch()
  renderInThisArticle()

  await Promise.all([
    renderMarkdown(),
    renderNav(),
    highlight()
  ])

  window.docfx.ready = true

  async function renderNav() {
    const [navbar, toc] = await Promise.all([renderNavbar(), renderToc()])
    renderBreadcrumb([...navbar, ...toc])
  }
}
