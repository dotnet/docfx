// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const esbuild = require('esbuild')
const CleanCSS = require('clean-css')
const { writeFileSync, cpSync } = require('fs')

async function build() {

  esbuild.build({
    bundle: true,
    minify: true,
    outfile: 'default/styles/docfx.vendor.js',
    entryPoints: [
      'src/main.js'
    ]
  })

  await minifyCss('default/styles/docfx.vendor.css', [
    'node_modules/bootstrap/dist/css/bootstrap.css',
    'node_modules/highlight.js/styles/github.css'
  ])

  cpSync('node_modules/bootstrap/dist/fonts', 'default/fonts', { recursive: true })

  copyToDist()
}

function copyToDist() {
  cpSync('common', 'dist/common', { recursive: true, overwrite: true, filter });
  cpSync('common', 'dist/default', { recursive: true, overwrite: true, filter });
  cpSync('common', 'dist/pdf.default', { recursive: true, overwrite: true, filter });
  cpSync('common', 'dist/statictoc', { recursive: true, overwrite: true, filter });

  cpSync('default', 'dist/default', { recursive: true, overwrite: true, filter });
  cpSync('default', 'dist/pdf.default', { recursive: true, overwrite: true, filter });
  cpSync('default', 'dist/statictoc', { recursive: true, overwrite: true, filter: staticTocFilter });

  cpSync('default(zh-cn)', 'dist/default(zh-cn)', { recursive: true, overwrite: true, filter });
  cpSync('pdf.default', 'dist/pdf.default', { recursive: true, overwrite: true, filter });
  cpSync('statictoc', 'dist/statictoc', { recursive: true, overwrite: true, filter });

  function filter(src) {
    return !src.includes('node_modules') && !src.includes('package-lock.json');
  }

  function staticTocFilter(src) {
    return filter(src) && !src.includes('toc.html');
  }
}

async function minifyCss(outputFile, inputFiles) {
  var result = new CleanCSS().minify(inputFiles);
  writeFileSync(outputFile, result.styles)
}

build()
