// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { html } from 'lit-html'
import { Theme } from './options'
import { loc, options } from './helper'

function setTheme(theme: Theme) {
  localStorage.setItem('theme', theme)
  if (theme === 'auto') {
    document.documentElement.setAttribute('data-bs-theme', window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')
  } else {
    document.documentElement.setAttribute('data-bs-theme', theme)
  }
}

async function getDefaultTheme() {
  return localStorage.getItem('theme') as Theme || (await options()).defaultTheme || 'auto'
}

export async function initTheme() {
  setTheme(await getDefaultTheme())
}

export function onThemeChange(callback: (theme: 'light' | 'dark') => void) {
  return new MutationObserver(() => callback(getTheme()))
    .observe(document.documentElement, { attributes: true, attributeFilter: ['data-bs-theme'] })
}

export function getTheme(): 'light' | 'dark' {
  return document.documentElement.getAttribute('data-bs-theme') as 'light' | 'dark'
}

export async function themePicker(refresh: () => void) {
  const theme = await getDefaultTheme()
  const icon = theme === 'light' ? 'sun' : theme === 'dark' ? 'moon' : 'circle-half'

  return html`
    <div class='dropdown'>
      <a title='${loc('changeTheme')}' class='btn border-0 dropdown-toggle' data-bs-toggle='dropdown' aria-expanded='false'>
        <i class='bi bi-${icon}'></i>
      </a>
      <ul class='dropdown-menu dropdown-menu-end'>
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
