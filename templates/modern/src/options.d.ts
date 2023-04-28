// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import BootstrapIcons from 'bootstrap-icons/font/bootstrap-icons.json'
import { HLJSApi } from 'highlight.js'
import { AnchorJSOptions } from 'anchor-js'
import { MermaidConfig } from 'mermaid'

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

  /** Configures [hightlight.js](https://highlightjs.org/) */
  configureHljs?: (hljs: HLJSApi) => void,
}
