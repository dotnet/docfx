// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

require('@default/bootstrap/dist/css/bootstrap.css')
require('@default/highlight.js/styles/github.css')

window.$ = window.jQuery = require('@default/jquery')

require('@default/bootstrap')
require('@default/twbs-pagination')
require('@default/mark.js/src/jquery.js')

const AnchorJS = require('@default/anchor-js')
window.anchors = new AnchorJS()

window.hljs = require('@default/highlight.js')
require('@default/url/src/url.js')
