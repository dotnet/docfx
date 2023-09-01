// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { html } from 'lit-html'
import { Theme } from './options'
import { loc } from './helper'

function setTheme(theme: Theme) {
  localStorage.setItem('theme', theme)
  if (theme === 'auto') {
    document.documentElement.setAttribute('data-bs-theme', window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')
  } else {
    document.documentElement.setAttribute('data-bs-theme', theme)
  }
}

function getDefaultTheme() {
  return localStorage.getItem('theme') as Theme || window.docfx.defaultTheme || 'auto'
}

export function initTheme() {
  setTheme(getDefaultTheme())
}

export function getTheme(): 'light' | 'dark' {
  return document.documentElement.getAttribute('data-bs-theme') as 'light' | 'dark'
}

export function themePicker(refresh: () => void) {
  const theme = getDefaultTheme()
  const icon = theme === 'light' ? 'sun' : theme === 'dark' ? 'moon' : 'circle-half'

  return html`
    <div class='dropdown'>
      <a title='Change theme' class='btn border-0 dropdown-toggle' data-bs-toggle='dropdown' aria-expanded='false'>
        <i class='bi bi-${icon}'></i>
      </a>
      <ul class='dropdown-menu'>
        <li><a class='dropdown-item' href='#' @click=${e => changeTheme(e, 'light')}><i class='bi bi-sun'></i> ${loc('themeLight')}</a></li>
        <li><a class='dropdown-item' href='#' @click=${e => changeTheme(e, 'dark')}><i class='bi bi-moon'></i> ${loc('themeDark')}</a></li>
        <li><a class='dropdown-item' href='#' @click=${e => changeTheme(e, 'auto')}><i class='bi bi-circle-half'></i> ${loc('themeAuto')}</a></li>
      </ul>
    </div>`

  function changeTheme(e, theme: Theme) {
    e.preventDefault()
    setTheme(theme)
    refresh()
  }
}
