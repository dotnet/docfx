// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export function meta(name: string): string {
  return (document.querySelector(`meta[name="${name}"]`) as HTMLMetaElement)?.content
}

/**
 * Add <wbr> into long word.
 * @param {String} text - The word to break. It should be in plain text without HTML tags.
 */
function breakPlainText(text) {
  if (!text) return text
  return text.replace(/([a-z])([A-Z])|(\.)(\w)/g, '$1$3<wbr>$2$4')
}

/**
 * Add <wbr> into long word.
 */
function breakWord(e: Element) {
  if (!e.innerHTML.match(/(<\w*)((\s\/>)|(.*<\/\w*>))/g)) {
    e.innerHTML = breakPlainText(e.innerHTML)
  }
}

export function breakText() {
  document.querySelectorAll('.xref').forEach(e => e.classList.add('text-break'))
  document.querySelectorAll('.text-break').forEach(e => breakWord(e))
}
