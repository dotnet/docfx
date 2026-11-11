// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import BootstrapIcons from 'bootstrap-icons/font/bootstrap-icons.json'
import { HLJSApi } from 'highlight.js'
import { AnchorJSOptions } from 'anchor-js'
import { MermaidConfig } from 'mermaid'

export type Theme = 'light' | 'dark' | 'auto'

/**
 * An entry of the version selector, see [multiple versions and locales](https://dotnet.github.io/docfx/docs/versioning.html).
 */
export type VersionOption = {
  /** The display label of this version shown in the version selector. */
  label: string,

  /** The base URL of this version's site, resolved against the page URL, e.g. `/repo/v1.0/`. */
  url: string,

  /**
   * The [monikers](https://dotnet.github.io/docfx/docs/markdown.html#moniker-sections) this
   * version covers, e.g. `v1.0 v1.1`. Moniker sections of the current page that don't apply
   * to this version are hidden.
   */
  moniker?: string,
}

/**
 * An entry of the locale selector, see [locales](https://dotnet.github.io/docfx/docs/locales.html).
 */
export type LocaleOption = {
  /** The display label of this locale shown in the locale selector. */
  label: string,

  /** The base URL of this locale's site, resolved against the page URL, e.g. `/repo/ja/`. */
  url: string,
}

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

  /**
   * Enables the [version selector](https://dotnet.github.io/docfx/docs/versioning.html) shown in the navbar,
   * for sites that publish one documentation set per version.
   *
   * Either the URL of a `versions.json` manifest, or the entries inline:
   *
   * ```js
   * export default {
   *   versions: '/repo/versions.json',
   *   // or
   *   versions: [
   *     { label: 'main', url: '/repo/main/' },
   *     { label: 'v2.0 (stable)', url: '/repo/' },
   *   ],
   * }
   * ```
   */
  versions?: string | VersionOption[],

  /**
   * Enables the [locale selector](https://dotnet.github.io/docfx/docs/locales.html)
   * shown in the navbar, for sites that publish one documentation set per locale.
   *
   * Either the URL of a `locales.json` manifest, or the entries inline:
   *
   * ```js
   * export default {
   *   locales: '/repo/locales.json',
   *   // or
   *   locales: [
   *     { label: 'English', url: '/repo/en/' },
   *     { label: '日本語', url: '/repo/ja/' },
   *   ],
   * }
   * ```
   */
  locales?: string | LocaleOption[],

  /** Hooks to app start event */
  start?: () => void,

  /** Configures [hightlight.js](https://highlightjs.org/) */
  configureHljs?: (hljs: HLJSApi) => void,

  /** Specifies if an image should show as lightbox when clicked. */
  showLightbox?: (image: HTMLImageElement) => boolean,
}
