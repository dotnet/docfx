// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const esbuild = require('esbuild')
const { sassPlugin } = require('esbuild-sass-plugin')
const bs = require('browser-sync')
const { cpSync, readdirSync, lstatSync, rmSync } = require('fs')
const { join } = require('path')
const { spawnSync } = require('child_process')
const yargs = require('yargs/yargs')
const { hideBin } = require('yargs/helpers')
const argv = yargs(hideBin(process.argv)).argv

const watch = argv.watch
const project = '../samples/seed'

build()

async function build() {

  await esbuild.build({
    bundle: true,
    minify: true,
    sourcemap: true,
    outExtension: {
      '.css': '.min.css',
      '.js': '.min.js'
    },
    outdir: '.',
    entryPoints: {
      'default/styles/docfx.vendor': 'default/src/main.js',
      'default/styles/search-worker': 'default/src/search-worker.js',
      'modern/styles/docfx': 'modern/src/docfx.ts',
      'modern/styles/search-worker': 'modern/src/search-worker.ts',
    },
    plugins: [
      sassPlugin()
    ],
    loader: {
      '.eot': 'file',
      '.svg': 'file',
      '.ttf': 'file',
      '.woff': 'file',
      '.woff2': 'file'
    },
    watch: watch && {
      onRebuild(error, result) {
        if (error) {
          console.error('watch build failed:', error)
        } else {
          console.log('watch build succeeded:', result)
        }
      }
    }
  })

  copyToDist()

  if (watch) {
    serve()
  }
}

function copyToDist() {

  readdirSync('dist')
    .filter(d => lstatSync(join('dist', d)).isDirectory())
    .forEach(d => rmSync(join('dist', d), { recursive: true, force: true }))
  
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
  cpSync('modern', 'dist/modern', { recursive: true, overwrite: true, filter });

  function filter(src) {
    const segments = src.split(/[/\\]/);
    return !segments.includes('node_modules') && !segments.includes('package-lock.json') && !segments.includes('src');
  }

  function staticTocFilter(src) {
    return filter(src) && !src.includes('toc.html');
  }
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
      'default/styles/**',
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
