// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import esbuild from 'esbuild'
import { sassPlugin } from 'esbuild-sass-plugin'
import bs from 'browser-sync'
import { cpSync, rmSync } from 'fs'
import { readFile, writeFile } from 'fs/promises'
import { join } from 'path'
import { spawnSync } from 'child_process'
import rtlcss from 'rtlcss'
import yargs from 'yargs/yargs'
import { hideBin } from 'yargs/helpers'
import { patchLunrLanguage } from './lunr-languages.js'

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
      'modern/src/search.ts',
    ],
    external: [
      './main.js'
    ],
    plugins: [
      sassPlugin({
        quetDeps: true,
        silenceDeprecations: ['import', 'global-builtin', 'color-functions'],
      }),
      {
        name: 'Exclude `./search` script from bundle',
        setup(build) {
          build.onResolve({ filter: /^\.\/search$/ }, args => {
            return {
              path: './search.min.js',
              external: true
            };
          });
        }
      },
      {
        // The Arabic and Hebrew stemmers of lunr-languages rely on `this` being the
        // global object, which is not the case in the strict ES module bundle
        name: 'Fix lunr-languages stemmers for ES modules',
        setup(build) {
          build.onLoad({ filter: /[\\/]lunr-languages[\\/]lunr\.(ar|he)\.js$/ }, async ({ path }) => {
            const source = await readFile(path, 'utf8')
            const patched = patchLunrLanguage(source)
            if (patched === null) {
              console.warn(`${path}: nothing to patch. If lunr-languages fixed its stemmers, remove this plugin and templates/lunr-languages.js.`)
              return
            }
            return { contents: patched, loader: 'js' }
          })
        }
      },
      {
        name: 'Right-to-left stylesheet',
        setup(build) {
          build.onEnd(result => result.errors.length === 0 && buildRtlStylesheet())
        }
      }
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

/**
 * The stylesheet for right-to-left pages: the left-to-right styles with their left and
 * right mirrored, as done for Bootstrap's own RTL stylesheet, followed by the rules that
 * must not be mirrored, such as code being left-to-right.
 */
async function buildRtlStylesheet() {
  const ltr = await readFile('modern/public/docfx.min.css', 'utf8')
  const direction = (await esbuild.transform(await readFile('modern/src/direction.css', 'utf8'), { loader: 'css', minify: true })).code

  // The source map belongs to the left-to-right stylesheet only
  const rtl = rtlcss.process(ltr).replace(/\/\*# sourceMappingURL=\S+ \*\/\s*$/, '')
  await writeFile('modern/public/docfx.rtl.min.css', /^@charset/.test(rtl) ? rtl + direction : `@charset "UTF-8";${rtl}${direction}`)
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
  exec(`dotnet run -f net10.0 --project ../src/docfx/docfx.csproj -- metadata ${project}/docfx.json`)
  exec(`dotnet run -f net10.0 --project ../src/docfx/docfx.csproj --no-build -- build ${project}/docfx.json`)

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
