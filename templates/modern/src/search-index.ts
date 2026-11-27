// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import lunr from 'lunr'
import { multiLanguage } from './search-languages'

export type SearchHit = {
  href: string
  title: string
  summary: string
  keywords: string
}

/**
 * Words are separated by whitespace and punctuation, and by the zero-width non-joiner
 * that Persian uses to join the parts of a word, e.g. می‌شود.
 */
export const separator = /[\s\-.()\u200C]+/

/**
 * lunr's trimmer keeps `[A-Za-z0-9_]` only: it trims a token that consists of other
 * letters, such as a Persian or Cyrillic word, to an empty string, so that the word is
 * never indexed. Keep every letter, digit, combining mark and underscore instead.
 */
export function trimmer(token: lunr.Token): lunr.Token {
  return token.update(word => word.replace(/^[^\p{L}\p{N}\p{M}_]+/u, '').replace(/[^\p{L}\p{N}\p{M}_]+$/u, ''))
}

lunr.Pipeline.registerFunction(trimmer, 'docfx-trimmer')

/**
 * Replaces the trimmer of the pipeline: lunr's own trimmers keep the characters of English
 * or of the configured languages only, and drop the words of every other language.
 */
function useUnicodeTrimmer(pipeline: lunr.Pipeline, replace: lunr.PipelineFunction, insertBefore: lunr.PipelineFunction) {
  pipeline.remove(replace)
  pipeline.before(insertBefore, trimmer)
}

/**
 * The functions that a plugin adds to the pipeline of an index.
 */
function pipelineFunctions(plugin: lunr.Builder.Plugin): lunr.PipelineFunction[] {
  const functions: lunr.PipelineFunction[] = []
  plugin.call({
    pipeline: {
      reset: () => { functions.length = 0 },
      add: (...added: lunr.PipelineFunction[]) => { functions.push(...added) }
    },
    searchPipeline: { reset: () => { }, add: () => { } }
  } as unknown as lunr.Builder)
  return functions
}

/**
 * Builds the search index of the documents. Their words are analyzed for the given
 * languages, which needs their lunr language packs, or for English by default. Words in
 * other languages are indexed as they are written.
 */
export function createIndex(data: { [key: string]: SearchHit }, languages: string[] = []): lunr.Index {
  lunr.tokenizer.separator = separator

  return lunr(function() {
    this.ref('href')
    this.field('title', { boost: 50 })
    this.field('keywords', { boost: 40 })
    this.field('summary', { boost: 20 })

    // English alone is the default pipeline. Its trimmer also keeps the words of
    // languages without a language pack, which the multi-language trimmer doesn't.
    if (languages.length > 1 || (languages.length === 1 && languages[0] !== 'en')) {
      const plugin = multiLanguage(lunr, languages)
      this.use(plugin)

      // The multi-language trimmer is the first function of the pipeline. It keeps the
      // characters of the configured languages only.
      this.pipeline.reset()
      this.pipeline.add(trimmer, ...pipelineFunctions(plugin).slice(1))
    } else {
      useUnicodeTrimmer(this.pipeline, lunr.trimmer, lunr.stopWordFilter)
    }

    for (const key in data) {
      this.add(data[key])
    }
  })
}
