const { minify } = require('terser')
const CleanCSS = require('clean-css')
const { readFileSync, writeFileSync, cpSync } = require('fs')

async function build() {
  await minifyJs(
    'default/styles/docfx.vendor.js', [
    'node_modules/jquery/dist/jquery.min.js',
    'node_modules/bootstrap/dist/js/bootstrap.min.js',
    'node_modules/highlightjs/highlight.pack.min.js',
    'node_modules/@websanova/url/dist/url.min.js',
    'node_modules/twbs-pagination/jquery.twbsPagination.min.js',
    'node_modules/mark.js/dist/jquery.mark.min.js',
    'node_modules/anchor-js/anchor.min.js'])

  await minifyJs('default/styles/lunr.min.js', [
    'node_modules/lunr/lunr.js'])

  await minifyCss('default/styles/docfx.vendor.css', [
    'node_modules/bootstrap/dist/css/bootstrap.css',
    'node_modules/highlightjs/styles/github-gist.css'
  ])

  cpSync('node_modules/bootstrap/dist/fonts', 'default/fonts', { recursive: true })
}

async function minifyJs(outputFile, inputFiles) {
  const code = Object.fromEntries(
    inputFiles.map(file => [file, readFileSync(file).toString()]))
  const result = await minify(code)
  writeFileSync(outputFile, result.code)
}

async function minifyCss(outputFile, inputFiles) {
  var result = new CleanCSS().minify(inputFiles);
  writeFileSync(outputFile, result.styles)
}

build()
