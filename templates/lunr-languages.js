// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * lunr-languages/lunr.ar.js and lunr.he.js keep the state of their stemmers on `this` of a
 * function that is called without a receiver:
 *
 *     lunr.ar.stemmer = (function() {
 *       var self = this
 *       self.result = false
 *       ...
 *
 * In a classic script, `this` is the global object. Bundled as an ES module, the code runs in
 * strict mode where `this` is undefined: "Cannot set properties of undefined (setting 'result')".
 * The state is kept in an object instead.
 *
 * Returns the patched source, or null when there is nothing to patch.
 */
export function patchLunrLanguage(source) {
  if (!source.includes('var self = this;')) {
    return null
  }
  return source.replaceAll('var self = this;', 'var self = {};')
}
