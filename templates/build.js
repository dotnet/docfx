// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

const esbuild = require('esbuild')
const { sassPlugin } = require('esbuild-sass-plugin')
const bs = require('browser-sync')
const { cpSync, rmSync } = require('fs')
const { join } = require('path')
const { spawnSync } = require('child_process')
const yargs = require('yargs/yargs')
const { hideBin } = require('yargs/helpers')
const argv = yargs(hideBin(process.argv)).argv

const watch = argv.watch
const project = argv.project || '../samples/seed'
const distdir = '../src/Docfx.App/templates'

const loader = {
  '.eot': 'file',
  '.svg': 'file',
  '.ttf': 'file',
  '.woff': 'file',
  '.woff2': 'file'
}

build()

async function build() {

  await Promise.all([buildDefaultTemplate(), buildModernTemplate()])

  copyToDist()

  if (watch) {
    serve()
  }
}

async function buildModernTemplate() {
  const config = {
    bundle: true,
    format: 'esm',
    splitting: true,
    minify: true,
    sourcemap: true,
    outExtension: {
      '.css': '.min.css',
      '.js': '.min.js'
    },
    outdir: 'modern/public',
    entryPoints: [
      'modern/src/docfx.ts',
      'modern/src/search-worker.ts',
    ],
    external: [
      './main.js'
    ],
    plugins: [
      sassPlugin()
    ],
    loader,
  }

  if (watch) {
    const context = await esbuild.context(config)
    await context.watch()
  } else {
    await esbuild.build(config)
  }
}

async function buildDefaultTemplate() {
  await esbuild.build({
    bundle: true,
    minify: true,
    sourcemap: true,
    outExtension: {
      '.css': '.min.css',
      '.js': '.min.js'
    },
    outdir: 'default/styles',
    entryPoints: [
      'default/src/docfx.vendor.js',
      'default/src/search-worker.js',
    ],
    loader
  })
}

function copyToDist() {

  rmSync(distdir, { recursive: true, force: true })

  cpSync('common', join(distdir, 'common'), { recursive: true, overwrite: true, filter })
  cpSync('common', join(distdir, 'default'), { recursive: true, overwrite: true, filter })
  cpSync('common', join(distdir, 'statictoc'), { recursive: true, overwrite: true, filter })

  cpSync('default', join(distdir, 'default'), { recursive: true, overwrite: true, filter })
  cpSync('default', join(distdir, 'statictoc'), { recursive: true, overwrite: true, filter: staticTocFilter })

  cpSync('default(zh-cn)', join(distdir, 'default(zh-cn)'), { recursive: true, overwrite: true, filter })
  cpSync('statictoc', join(distdir, 'statictoc'), { recursive: true, overwrite: true, filter })
  cpSync('modern', join(distdir, 'modern'), { recursive: true, overwrite: true, filter })

  function filter(src) {
    const segments = src.split(/[/\\]/)
    return !segments.includes('node_modules') && !segments.includes('package-lock.json') && !segments.includes('src')
  }

  function staticTocFilter(src) {
    return filter(src) && !src.includes('toc.html')
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
      'modern/public/**',
      join(project, '_site', '**')
    ],
    server: {
      routes: {
        '/test/public/main.js': join(project, '_site', 'public', 'main.js'),
        '/test/public': 'modern/public',
        '/test': join(project, '_site')
      }
    }
  })
}
