/**
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 */

export default {
  start: () => {
    const element = document.getElementById('sdk-compatibility-report')
    if (element) import('./sdk-compatibility.mjs').catch(() => {
      element.textContent = 'Compatibility evidence unavailable. The report viewer could not be loaded.'
    })
  },
  iconLinks: [
    {
      icon: 'github',
      href: 'https://github.com/dotnet/docfx',
      title: 'GitHub'
    }
  ]
}
