// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { html, render } from 'lit-html'

setTheme(localStorage.getItem('theme') || 'auto')

function setTheme(theme) {
  if (theme === 'auto') {
    document.body.setAttribute('data-bs-theme', window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')
  } else {
    document.body.setAttribute('data-bs-theme', theme)
  }
}

export function renderThemePicker() {
  const footer = document.querySelector('footer>div') as HTMLElement

  renderCore(localStorage.getItem('theme') || 'auto')

  function renderCore(theme) {
    setTheme(theme)

    if (footer) {
      const icon = theme === 'light' ? 'sun' : theme === 'dark' ? 'moon' : 'circle-half'
      const themePicker = html`
        <div class='dropdown'>
          <button class='btn border-0 dropdown-toggle' type='button' data-bs-toggle='dropdown' aria-expanded='false'>
            <i class='bi bi-${icon}'></i>
          </button>
          <ul class='dropdown-menu'>
            <li><a class='dropdown-item' href='#' @click=${e => { e.preventDefault(); renderCore('light') }}><i class='bi bi-sun'></i> Light</a></li>
            <li><a class='dropdown-item' href='#' @click=${e => { e.preventDefault(); renderCore('dark') }}><i class='bi bi-moon'></i> Dark</a></li>
            <li><a class='dropdown-item' href='#' @click=${e => { e.preventDefault(); renderCore('auto') }}><i class='bi bi-circle-half'></i> Auto</a></li>
          </ul>
        </div>`

      render(themePicker, footer)
    }
  }
}
