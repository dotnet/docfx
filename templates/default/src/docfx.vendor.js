// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

require('@default/bootstrap/dist/css/bootstrap.css')
require('@default/highlight.js/styles/github.css')

window.$ = window.jQuery = require('jquery')

require('@default/bootstrap')
require('@default/twbs-pagination')
require('@default/mark.js/src/jquery')

const AnchorJS = require('@default/anchor-js')
window.anchors = new AnchorJS()

window.hljs = require('@default/highlight.js')
require('@default/url/src/url.js')
