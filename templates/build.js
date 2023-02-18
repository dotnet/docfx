// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const esbuild = require('esbuild')
const bs = require('browser-sync')
const CleanCSS = require('clean-css')
const { writeFileSync, cpSync } = require('fs')
const { join } = require('path')
const { spawnSync } = require('child_process')
const yargs = require('yargs/yargs')
const { hideBin } = require('yargs/helpers')
const argv = yargs(hideBin(process.argv)).argv

const watch = argv.watch
const project = '../samples/seed'

build()

async function build() {

  esbuild.build({
    bundle: true,
    minify: true,
    sourcemap: true,
    outdir: 'default/styles',
    outExtension: {
      '.css': '.min.css',
      '.js': '.min.js'
    },
    entryPoints: [
      'src/docfx.js',
      'src/search-worker.js'
    ]
  })

  await minifyCss('default/styles/docfx.min.css', [
    'src/docfx.css',
    'node_modules/bootstrap/dist/css/bootstrap.css',
    'node_modules/highlight.js/styles/github.css'
  ])

  cpSync('node_modules/bootstrap/dist/fonts', 'default/fonts', { recursive: true })

  copyToDist()

  if (watch) {
    serve()
  }
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

function buildContent() {
  exec(`dotnet run -f net7.0 --project ../src/docfx/docfx.csproj -- metadata ${project}/docfx.json`)
  exec(`dotnet run -f net7.0 --project ../src/docfx/docfx.csproj --no-build -- build ${project}/docfx.json`)

  function exec(cmd) {
    if (spawnSync(cmd, { stdio: 'inherit', shell: true }).status !== 0) {
      throw Error(`exec error: '${cmd}'`)
    }
  }
}

function serve() {
  buildContent()

  return bs.create('docfx').init({
    open: true,
    startPath: '/test',
    files: [
      'src/**',
      join(project, '_site', '**')
    ],
    server: {
      routes: {
        '/test/styles': 'default/styles',
        '/test': join(project, '_site')
      }
    }
  })
}
