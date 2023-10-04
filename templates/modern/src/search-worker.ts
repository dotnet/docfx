// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import lunr from 'lunr'
import { get, set, createStore } from 'idb-keyval'

type SearchHit = {
  href: string
  title: string
  keywords: string
}

let search: (q: string) => SearchHit[]

async function loadIndex() {
  const { index, data } = await loadIndexCore()
  search = q => index.search(q).map(({ ref }) => data[ref])
  postMessage({ e: 'index-ready' })
}

async function loadIndexCore() {
  const res = await fetch('../index.json')
  const etag = res.headers.get('etag')
  const data = await res.json() as { [key: string]: SearchHit }
  const cache = createStore('docfx', 'lunr')

  if (etag) {
    const value = JSON.parse(await get('index', cache) || '{}')
    if (value && value.etag === etag) {
      return { index: lunr.Index.load(value), data }
    }
  }

  const { configureLunr } = await import('./main.js').then(m => m.default) as DocfxOptions

  const index = lunr(function() {
    this.pipeline.remove(lunr.stopWordFilter)
    this.ref('href')
    this.field('title', { boost: 50 })
    this.field('keywords', { boost: 20 })

    lunr.tokenizer.separator = /[\s\-.()]+/
    configureLunr?.(this)

    for (const key in data) {
      this.add(data[key])
    }
  })

  if (etag) {
    await set('index', JSON.stringify(Object.assign(index.toJSON(), { etag })), cache)
  }

  return { index, data }
}

loadIndex().catch(console.error)

onmessage = function(e) {
  if (search) {
    postMessage({ e: 'query-ready', d: search(e.data.q) })
  }
}
