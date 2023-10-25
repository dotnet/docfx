// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import lunr from 'lunr'
import stemmer from 'lunr-languages/lunr.stemmer.support'
import multi from 'lunr-languages/lunr.multi'
import { get, set, createStore } from 'idb-keyval'

type SearchHit = {
  href: string
  title: string
  keywords: string
}

let search: (q: string) => SearchHit[]

async function loadIndex({ lunrLanguages }: { lunrLanguages?: string[] }) {
  const { index, data } = await loadIndexCore()
  search = q => index.search(q).map(({ ref }) => data[ref])
  postMessage({ e: 'index-ready' })

  async function loadIndexCore() {
    const res = await fetch('../index.json')
    const etag = res.headers.get('etag')
    const data = await res.json() as { [key: string]: SearchHit }
    const cache = createStore('docfx', 'lunr')

    if (lunrLanguages && lunrLanguages.length > 0) {
      multi(lunr)
      stemmer(lunr)
      await Promise.all(lunrLanguages.map(initLanguage))
    }

    if (etag) {
      const value = JSON.parse(await get('index', cache) || '{}')
      if (value && value.etag === etag) {
        return { index: lunr.Index.load(value), data }
      }
    }

    const index = lunr(function() {
      lunr.tokenizer.separator = /[\s\-.()]+/

      this.ref('href')
      this.field('title', { boost: 50 })
      this.field('keywords', { boost: 20 })

      if (lunrLanguages && lunrLanguages.length > 0) {
        this.use(lunr.multiLanguage(...lunrLanguages))
      }

      for (const key in data) {
        this.add(data[key])
      }
    })

    if (etag) {
      await set('index', JSON.stringify(Object.assign(index.toJSON(), { etag })), cache)
    }

    return { index, data }
  }
}

onmessage = function(e) {
  if (e.data.q && search) {
    postMessage({ e: 'query-ready', d: search(e.data.q) })
  } else if (e.data.init) {
    loadIndex(e.data.init).catch(console.error)
  }
}

const langMap = {
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

async function initLanguage(lang: string) {
  if (lang !== 'en') {
    const { default: init } = await langMap[lang]()
    init(lunr)
  }
}
