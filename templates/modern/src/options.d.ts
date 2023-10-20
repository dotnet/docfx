// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import BootstrapIcons from 'bootstrap-icons/font/bootstrap-icons.json'
import { HLJSApi } from 'highlight.js'
import { AnchorJSOptions } from 'anchor-js'
import { MermaidConfig } from 'mermaid'
import lunr from 'lunr'

export type Theme = 'light' | 'dark' | 'auto'

export type IconLink = {
  /** A [bootstrap-icons](https://icons.getbootstrap.com/) name */
  icon: keyof typeof BootstrapIcons,

  /** The URL of this icon link */
  href: string,

  /** The title of this icon link shown on mouse hover */
  title?: string
}

/**
 * Enables customization of the website through the global `window.docfx` object.
 */
export type DocfxOptions = {
  /** Configures the default theme */
  defaultTheme?: Theme,

  /** A list of icons to show in the header next to the theme picker */
  iconLinks?: IconLink[],

  /** Configures [anchor-js](https://www.bryanbraun.com/anchorjs#options) options */
  anchors?: AnchorJSOptions,

  /** Configures mermaid diagram options */
  mermaid?: MermaidConfig,

  /** A list of [lunr languages](https://github.com/MihaiValentin/lunr-languages#readme) such as fr, es for full text search */
  lunrLanguages?: string[],

  /** Configures [hightlight.js](https://highlightjs.org/) */
  configureHljs?: (hljs: HLJSApi) => void,

  /** Configures [lunr](https://lunrjs.com/docs/index.html) */
  configureLunr?: (lunr: lunr.Builder) => void,
}
