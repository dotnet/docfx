// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export function meta(name: string): string {
  return (document.querySelector(`meta[name="${name}"]`) as HTMLMetaElement)?.content
}

export function isVisible(element: Element): boolean {
  return (element as HTMLElement).offsetParent != null
}

export function getAbsolutePath(href: string): string {
  if (isAbsolutePath(href)) {
    return href
  }
  const currentAbsPath = getCurrentWindowAbsolutePath()
  const stack = currentAbsPath.split('/')
  stack.pop()
  const parts = href.split('/')
  for (let i = 0; i < parts.length; i++) {
    if (parts[i] === '.') continue
    if (parts[i] === '..' && stack.length > 0) {
      stack.pop()
    } else {
      stack.push(parts[i])
    }
  }
  return stack.join('/')
}

export function isRelativePath(href: string): boolean {
  if (href === undefined || href === '' || href[0] === '/') {
    return false
  }
  return !isAbsolutePath(href)
}

export function isAbsolutePath(href: string): boolean {
  return (/^(?:[a-z]+:)?\/\//i).test(href)
}

export function getDirectory(href: string): string {
  if (!href) return '.'
  const index = href.lastIndexOf('/')
  return index < 0 ? '.' : href.slice(0, index)
}

export function getCurrentWindowAbsolutePath() {
  return window.location.origin + window.location.pathname
}

export function formList(item, classes) {
  let level = 1
  const model = {
    items: item
  }
  const cls = [].concat(classes).join(' ')
  return getList(model, cls)

  function getList(model, cls) {
    if (!model || !model.items) return null
    const l = model.items.length
    if (l === 0) return null
    let html = '<ul class="level' + level + ' ' + (cls || '') + '">'
    level++
    for (let i = 0; i < l; i++) {
      const item = model.items[i]
      const href = item.href
      const name = item.name
      if (!name) continue
      html += href ? '<li><a href="' + href + '">' + name + '</a>' : '<li>' + name
      html += getList(item, cls) || ''
      html += '</li>'
    }
    html += '</ul>'
    return html
  }
}

/**
 * Add <wbr> into long word.
 * @param {String} text - The word to break. It should be in plain text without HTML tags.
 */
export function breakPlainText(text) {
  if (!text) return text
  return text.replace(/([a-z])([A-Z])|(\.)(\w)/g, '$1$3<wbr>$2$4')
}

/**
 * Add <wbr> into long word. The jQuery element should contain no html tags.
 * If the jQuery element contains tags, this function will not change the element.
 */
export function breakWord(e) {
  if (!e.html().match(/(<\w*)((\s\/>)|(.*<\/\w*>))/g)) {
    e.html(function(index, text) {
      return breakPlainText(text)
    })
  }
  return e
}
