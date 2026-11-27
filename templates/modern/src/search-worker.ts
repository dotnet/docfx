// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import lunr from 'lunr'
import stemmer from 'lunr-languages/lunr.stemmer.support'
import tinyseg from 'lunr-languages/tinyseg'
import multi from 'lunr-languages/lunr.multi'
import { get, set, createStore } from 'idb-keyval'
import { createIndex, SearchHit } from './search-index'
import { loadLanguages } from './search-languages'

/**
 * Indexes that were built before this version are not reused: e.g. older versions of docfx
 * did not index words in languages other than English.
 */
const indexVersion = 2

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
      if (lunrLanguages.includes('ja')) {
        tinyseg(lunr)
      }
      lunrLanguages = await loadLanguages(lunr, lunrLanguages)
    }

    if (etag) {
      const value = JSON.parse(await get('index', cache) || '{}')
      if (value && value.etag === etag && value.version === indexVersion) {
        return { index: lunr.Index.load(value), data }
      }
    }

    const index = createIndex(data, lunrLanguages)

    if (etag) {
      await set('index', JSON.stringify(Object.assign(index.toJSON(), { etag, version: indexVersion })), cache)
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
