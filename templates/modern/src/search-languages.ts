// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import lunr from 'lunr'

type LanguagePack = {
  default: (instance: typeof lunr) => void
}

const langMap: Record<string, () => Promise<LanguagePack>> = {
  ar: () => import('lunr-languages/lunr.ar.js'),
  da: () => import('lunr-languages/lunr.da.js'),
  de: () => import('lunr-languages/lunr.de.js'),
  du: () => import('lunr-languages/lunr.du.js'),
  el: () => import('lunr-languages/lunr.el.js'),
  es: () => import('lunr-languages/lunr.es.js'),
  fi: () => import('lunr-languages/lunr.fi.js'),
  fr: () => import('lunr-languages/lunr.fr.js'),
  he: () => import('lunr-languages/lunr.he.js'),
  hi: () => import('lunr-languages/lunr.hi.js'),
  hu: () => import('lunr-languages/lunr.hu.js'),
  hy: () => import('lunr-languages/lunr.hy.js'),
  it: () => import('lunr-languages/lunr.it.js'),
  ja: () => import('lunr-languages/lunr.ja.js'),
  jp: () => import('lunr-languages/lunr.jp.js'),
  kn: () => import('lunr-languages/lunr.kn.js'),
  ko: () => import('lunr-languages/lunr.ko.js'),
  nl: () => import('lunr-languages/lunr.nl.js'),
  no: () => import('lunr-languages/lunr.no.js'),
  pt: () => import('lunr-languages/lunr.pt.js'),
  ro: () => import('lunr-languages/lunr.ro.js'),
  ru: () => import('lunr-languages/lunr.ru.js'),
  sa: () => import('lunr-languages/lunr.sa.js'),
  sv: () => import('lunr-languages/lunr.sv.js'),
  ta: () => import('lunr-languages/lunr.ta.js'),
  te: () => import('lunr-languages/lunr.te.js'),
  th: () => import('lunr-languages/lunr.th.js'),
  tr: () => import('lunr-languages/lunr.tr.js'),
  vi: () => import('lunr-languages/lunr.vi.js')

  // zh is currently not supported due to dependency on NodeJS.
  // zh: () => import('lunr-languages/lunr.zh.js')
}

/**
 * Loads the lunr language packs of the languages, e.g. to stem their words and to
 * remove their stop words. English is built into lunr.
 *
 * Returns the languages that are available to `multiLanguage`. A language without a
 * language pack is skipped with a warning: its words are analyzed like English words.
 */
export async function loadLanguages(instance: typeof lunr, languages: string[] = [], loaders: Record<string, () => Promise<LanguagePack>> = langMap): Promise<string[]> {
  const loaded = await Promise.all(languages.map(async lang => {
    const load = loaders[lang]
    if (!load) {
      if (lang !== 'en') {
        console.warn(`docfx: Search does not support the language "${lang}", its words are analyzed like English words.`)
      }
      return lang === 'en' ? lang : null
    }

    try {
      const { default: init } = await load()
      init(instance)
      return lang
    } catch (e) {
      console.error(`docfx: Cannot load the search language "${lang}", its words are analyzed like English words.`, e)
      return null
    }
  }))

  return loaded.filter(lang => lang !== null)
}

/**
 * `multiLanguage` is added to lunr by the lunr-languages plugin.
 */
export function multiLanguage(instance: typeof lunr, languages: string[]): lunr.Builder.Plugin {
  return (instance as unknown as { multiLanguage: (...languages: string[]) => lunr.Builder.Plugin }).multiLanguage(...languages)
}
