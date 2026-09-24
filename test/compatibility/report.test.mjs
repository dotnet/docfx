// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import test from 'node:test'
import assert from 'node:assert/strict'
import { readFile, mkdtemp, writeFile, rm, mkdir, readdir, symlink } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { createServer } from 'node:http'
import { join } from 'node:path'
import { execFileSync, spawnSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'
import { latestReport, resolveMatrix, resolveStableRelease, trustedRun, findSiteRun, productionReportReady, prepareReport, main } from './report.mjs'
import { validateReport, renderReport, loadReport, createCaseSelection, caseLogUrl } from '../../docs/template/public/sdk-compatibility.mjs'

// Protocol fixtures are synthetic unit-test inputs, never published measurement results.
const now = Date.parse('2026-09-22T00:00:00Z')
const sha = 'a'.repeat(40)
const matrixConfig = JSON.parse(await readFile(new URL('./sdk-matrix.json', import.meta.url), 'utf8'))
const sdkIndex = { 'releases-index': ['8.0', '9.0', '10.0', '11.0'].map(channel => ({
  'channel-version': channel, 'latest-sdk': `${channel}.100${channel === '11.0' ? '-rc.1.123' : ''}`,
  'support-phase': channel === '11.0' ? 'go-live' : 'active',
})) }
function report(matrix = [{ channel: '10.0', sdk: '10.0.401', projectTfm: 'net10.0' }]) {
  const data = {
    schemaVersion: 1, generatedAt: '2026-09-21T00:00:00Z',
    source: { repository: 'dotnet/docfx', sha, runId: '42', runAttempt: '1', dirty: false },
    matrix,
    channels: ['stable', 'nightly'], scenarios: ['basic', 'razor'],
    stableSelection: { mode: 'latest-release', requestedVersion: '2.80.1', reason: 'Synthetic release discovery' },
  }
  data.results = data.matrix.flatMap(target => data.channels.flatMap(channel => data.scenarios.map(scenario => ({
    ...target, selectedSdk: target.sdk, channel, scenario,
    toolVersion: channel === 'stable' ? '2.80.1' : '2.80.2-preview.1', toolRuntimeTfm: 'net10.0',
    packageSha256: 'b'.repeat(64), os: 'Unit-test OS', testedAt: data.generatedAt,
    outcome: 'passed', diagnostics: 'Unit-test diagnostics', log: `logs/${channel}-${target.sdk}-${target.projectTfm}-${scenario}.log`,
  }))))
  return data
}
function multiToolReport(matrix = resolveMatrix(matrixConfig, sdkIndex)) {
  const data = report(matrix)
  data.schemaVersion = 2
  data.toolTargets = [
    { channel: 'stable', framework: 'net10.0' },
    ...['net8.0', 'net9.0', 'net10.0', 'net11.0'].map(framework => ({ channel: 'nightly', framework })),
  ]
  data.results = data.results.flatMap(row => data.toolTargets.filter(t => t.channel === row.channel).map(({ framework }) => ({
    ...row, toolFramework: framework, toolRuntimeTfm: framework,
    log: row.log.replace(`${row.channel}-`, `${row.channel}-${framework}-`),
  })))
  return data
}
function run(overrides = {}) {
  return { id: 42, workflow_id: 7, path: '.github/workflows/nightly.yml', head_repository: { full_name: 'dotnet/docfx' }, head_branch: 'main', head_sha: sha, event: 'schedule', status: 'completed', conclusion: 'failure', run_attempt: 1, ...overrides }
}
function artifact(overrides = {}) {
  return { id: 99, name: 'sdk-compatibility-v1', expired: false, size_in_bytes: 1000, workflow_run: { id: 42, head_sha: sha, head_branch: 'main' }, ...overrides }
}
const adapter = (runs = [run()], artifacts = [artifact()]) => async path => {
  if (path.endsWith('/nightly.yml')) return { id: 7 }
  if (path.includes('/artifacts?')) return { artifacts }
  return { workflow_runs: runs }
}

test('validates complete reports, including incompatible and infrastructure rows', () => {
  const data = report()
  data.results[1].outcome = 'incompatible'
  data.results[2].outcome = 'infrastructure-error'
  assert.equal(validateReport(data, data.source, now), data)
})

test('renders concise counts for every observed outcome without duplicate warnings', () => {
  const data = report()
  const passed = renderReport(data, now)
  assert.match(passed, /<strong>4 passed<\/strong> · 0 incompatible<\/p>/)
  assert.doesNotMatch(passed, /alert-warning|alert-danger|infrastructure error\(s\)|\d+ unavailable/)
  data.results[1].outcome = 'incompatible'
  data.results[2].outcome = 'infrastructure-error'
  data.results[3].outcome = 'unavailable'
  const html = renderReport(data, now)
  assert.match(html, /<strong>1 passed<\/strong> · 1 incompatible · 1 unavailable · 1 infrastructure error\(s\)<\/p>/)
  for (const outcome of ['passed', 'incompatible', 'unavailable', 'infrastructure-error']) assert.equal((html.match(new RegExp(`data-outcome="${outcome}"`, 'g')) ?? []).length, 1)
  assert.doesNotMatch(html, /Compatibility issues found|A completed report does not mean|support guarantee/)
})

test('rejects false passes, incomplete/duplicate rows, and invalid identity', () => {
  const mutations = [
    r => r.results.pop(), r => { r.results[1] = r.results[0] },
    r => { r.results[0].selectedSdk = '11.0.100' }, r => { r.results[0].selectedSdk = null },
    r => { r.results[0].packageSha256 = null }, r => { r.results[0].toolVersion = null },
    r => { r.results[0].toolRuntimeTfm = null }, r => { r.results[0].outcome = 'supported' },
    r => { r.results[0].log = '../evil.log' }, r => { r.results[0].toolVersion = '2.80.1-preview' },
    r => { r.results[1].packageSha256 = 'c'.repeat(64) }, r => { r.scenarios = ['basic'] },
    r => { r.source.dirty = true }, r => { r.schemaVersion = 2 },
  ]
  for (const mutate of mutations) {
    const data = report(); mutate(data)
    assert.throws(() => validateReport(data, undefined, now))
  }
})

test('rejects wrong source commit, run, attempt, and missing nightly channel', () => {
  for (const key of ['sha', 'runId', 'runAttempt', 'repository']) {
    const data = report()
    assert.throws(() => validateReport(data, { ...data.source, [key]: 'wrong' }, now))
  }
  const data = report()
  data.channels = ['stable']; data.results = data.results.filter(r => r.channel === 'stable')
  assert.throws(() => validateReport(data, data.source, now), /both package channels/)
})

test('rejects future and malformed timestamps, but renders stale evidence honestly', () => {
  const data = report()
  assert.match(renderReport(data, now + 8 * 86400000), /Stale evidence/)
  const html = renderReport(data, now)
  assert.doesNotMatch(html, /Stale evidence|alert-info/)
  assert.match(html, /Last tested <time datetime="2026-09-21T00:00:00Z">Sep 21, 2026<\/time>/)
  assert.match(html, /<details class="compatibility-provenance"><summary>Report source &amp; downloads<\/summary>/)
  assert.match(html, /Measured: 2026-09-21T00:00:00Z/)
  assert.match(html, /id="compatibility-case"[^>]* hidden>/)
  data.generatedAt = '2099-01-01T00:00:00Z'
  assert.throws(() => validateReport(data, undefined, now), /timestamp/)
  data.generatedAt = 'invalid'
  assert.throws(() => validateReport(data, undefined, now), /timestamp/)
})

test('report page keeps check scope collapsed and links to complete maintenance instructions', async () => {
  const page = await readFile(new URL('../../docs/docs/sdk-compatibility.md', import.meta.url), 'utf8')
  const maintenance = await readFile(new URL('../../docs/docs/sdk-compatibility-maintenance.md', import.meta.url), 'utf8')
  assert.match(page, /# SDK compatibility\s+<div id="sdk-compatibility-report">/)
  assert.match(page, /<details>\s*<summary>About these checks<\/summary>/)
  assert.doesNotMatch(page, /<details[^>]*\bopen\b|^## |```/m)
  for (const scope of ['**Basic**', '**Razor**', '**not a support guarantee**', 'Untested combinations', '**project target**', '**tool target framework**']) assert.ok(page.includes(scope))
  assert.match(page, /\(xref:sdk-compatibility-maintenance\)/)
  assert.match(maintenance, /^uid: sdk-compatibility-maintenance$/m)
  assert.match(maintenance, /\(xref:sdk-compatibility\)/)
  for (const instruction of [
    'node test/compatibility/report.mjs resolve drop/compatibility/matrix.json',
    './test/compatibility/Measure-Compatibility.ps1 -MatrixPath drop/compatibility/matrix.json -OutputDirectory drop/compatibility/report -NightlyPackage path/to/exact/docfx.nupkg',
    'node test/compatibility/report.mjs prepare drop/compatibility/report/compatibility-report.json docs/obj/sdk-compatibility.json',
    'docfx docs/docfx.json', 'node test/compatibility/report.mjs check path/to/compatibility-report.json',
    'node --test test/compatibility/report.test.mjs', '-NuGetConfig', '-FailOnIncompatible', '--strict',
    'validation_only', '**14 days**', 'roll-forward disabled', 'does not select only successful workflow runs',
  ]) assert.ok(maintenance.includes(instruction), `Missing maintenance instruction: ${instruction}`)
})

test('HTML-escapes report content and never creates artifact-controlled URLs', () => {
  const data = report()
  data.results[0].diagnostics = '<script>alert(1)</script>'
  data.results[0].os = '<img src=x onerror=alert(1)>'
  const html = renderReport(data, now)
  assert.ok(!html.includes('<script>'))
  assert.ok(!html.includes('<img'))
  assert.ok(!html.includes(data.results[0].diagnostics)) // Diagnostics are inserted as text only after selection.
  assert.ok(!html.includes(data.results[0].os))
  assert.match(html, /https:\/\/github.com\/dotnet\/docfx\/actions\/runs\/42\/attempts\/1/)
})

test('missing, expired, invalid and network-failed reports show no passing rows', async () => {
  const responses = [
    async () => ({ ok: false }),
    async () => ({ ok: true, json: async () => ({ state: 'unavailable', reason: 'Expired artifact' }) }),
    async () => ({ ok: true, json: async () => ({ schemaVersion: 999 }) }),
    async () => { throw new Error('Offline') },
  ]
  for (const fetchReport of responses) {
    const element = { textContent: '', innerHTML: '' }
    await loadReport(element, fetchReport)
    assert.match(element.textContent, /unavailable/)
    assert.equal(element.innerHTML, '')
  }
})

test('matrix headers preserve distinct tool targets, incomplete package identities, and older channels', () => {
  const data = report()
  for (const row of data.results.filter(r => r.channel === 'stable')) row.toolRuntimeTfm = 'net8.0'
  let html = renderReport(data, now)
  assert.equal((html.match(/2.80.1 &amp; net8.0<\/span>/g) ?? []).length, 2)
  assert.equal((html.match(/2.80.2-preview.1 &amp; net10.0<\/span>/g) ?? []).length, 2)
  const failure = data.results[3]
  failure.outcome = 'infrastructure-error'; failure.toolRuntimeTfm = 'net9.0'; failure.packageSha256 = null
  html = renderReport(data, now)
  assert.match(html, /Nightly &amp; Razor<\/strong><span class="compatibility-tool">2.80.2-preview.1 &amp; net9.0/)
  data.channels = ['nightly']; data.stableSelection = null
  data.results = data.results.filter(r => r.channel === 'nightly')
  html = renderReport(data, now)
  assert.doesNotMatch(html, /Stable &amp;/)
  assert.equal((html.match(/data-case-index=/g) ?? []).length, 2)
  for (const row of data.results) { row.toolVersion = null; row.toolRuntimeTfm = null; row.packageSha256 = null; row.outcome = 'unavailable' }
  html = renderReport(data, now)
  assert.equal((html.match(/Not measured &amp; Not measured/g) ?? []).length, 2)
  assert.match(html, /<strong>0 passed<\/strong> · 0 incompatible · 2 unavailable<\/p>/)
})

const logBase = new URL('https://example.test/docfx/reports/sdk-compatibility.json')
const deferred = () => { let resolve; const promise = new Promise(done => { resolve = done }); return { promise, resolve } }

test('case logs stay under the report directory and reject URLs, traversal and encoded paths', () => {
  assert.equal(caseLogUrl(report().results[0], logBase).href, 'https://example.test/docfx/reports/' + report().results[0].log)
  for (const log of ['../secret', 'logs/../secret.log', '/logs/a.log', 'https://evil.test/a.log', '//evil.test/a.log', 'logs/a.log?x=1', 'logs/%2e%2e.log', 'logs\\\\escape.log']) assert.throws(() => caseLogUrl({ log }, logBase))
  assert.throws(() => caseLogUrl(report().results[0], 'file:///tmp/report.json'))
})

test('details read only a selected log and preserve diagnostics, raw text and original download identity', async () => {
  const data = report()
  data.results[1].outcome = 'incompatible'
  data.results[1].diagnostics = '<script>unit diagnostic</script>'
  const output = '\u001b[31mwarning XX123: <img src=x onerror=alert(1)>\u001b[0m\r\nordinary unit output'
  const states = []; const requests = []
  const selection = createCaseSelection(data, state => states.push(state), async (url, options) => {
    requests.push([url.href, options])
    return { ok: true, url: url.href, text: async () => output }
  }, logBase)
  assert.equal(requests.length, 0)
  await selection.select(1)
  assert.equal(requests.length, 1)
  assert.equal(requests[0][1].redirect, 'error')
  assert.equal(requests[0][1].credentials, 'same-origin')
  assert.deepEqual(states.map(s => s.phase), ['loading', 'ready'])
  assert.equal(states[1].row.diagnostics, data.results[1].diagnostics)
  assert.equal(states[1].output, output.replace(/\u001b\[[0-9;]*m/g, ''))
  assert.equal(states[1].excerpt, 'warning XX123: <img src=x onerror=alert(1)>')
  assert.equal(states[1].logUrl, logBase.href.replace('sdk-compatibility.json', data.results[1].log))
  await selection.select(0)
  assert.equal(states.at(-1).excerpt, '')
  assert.equal(states.at(-1).row.outcome, 'passed')
  await assert.rejects(selection.select(99), /Unknown compatibility case/)
  selection.dispose()
})

test('late log responses cannot overwrite selection, close or disposal', async () => {
  const states = []; const delayed = deferred(); const started = deferred(); const signals = []
  const selection = createCaseSelection(report(), state => states.push(state), async (url, options) => {
    signals.push(options.signal)
    return { ok: true, text: () => signals.length === 1 ? (started.resolve(), delayed.promise) : Promise.resolve('current unit output') }
  }, logBase)
  const first = selection.select(0)
  await started.promise
  await selection.select(1)
  assert.equal(signals[0].aborted, true)
  delayed.resolve('late unit output')
  await first
  assert.equal(states.at(-1).index, 1)
  assert.equal(states.at(-1).output, 'current unit output')
  selection.close()
  assert.equal(states.at(-1).phase, 'closed')
  assert.equal(signals[1].aborted, true)
  selection.dispose()
  const count = states.length
  await selection.select(0)
  assert.equal(states.length, count)
  for (const action of ['close', 'dispose']) {
    const delayed = deferred(); const started = deferred(); const changes = []
    const active = createCaseSelection(report(), state => changes.push(state), async () => ({ ok: true, text: () => (started.resolve(), delayed.promise) }), logBase)
    const loading = active.select(0)
    await started.promise
    active[action]()
    const count = changes.length
    delayed.resolve('late unit output')
    await loading
    assert.equal(changes.length, count)
    active.dispose()
  }
})

test('log failures and retry leave the recorded outcome unchanged and reject redirects or oversized output', async () => {
  const data = report(); const states = []
  let response = { ok: false, status: 404 }
  const selection = createCaseSelection(data, state => states.push(state), async () => response, logBase)
  await selection.select(0)
  assert.equal(states.at(-1).phase, 'error')
  assert.match(states.at(-1).error, /404/)
  assert.equal(states.at(-1).row.outcome, 'passed')
  for (const failure of [
    { ok: true, redirected: true }, { ok: true, url: 'https://evil.test/log' },
    { ok: true, headers: new Headers({ 'content-length': 10 * 1024 * 1024 + 1 }) },
  ]) {
    response = failure
    await selection.select(0)
    assert.equal(states.at(-1).phase, 'error')
    assert.equal(data.results[0].outcome, 'passed')
  }
  response = { ok: true, text: async () => 'recovered unit output' }
  await selection.select(0)
  assert.equal(states.at(-1).phase, 'ready')
  assert.equal(states.at(-1).output, 'recovered unit output')
  selection.dispose()
})

test('a replaced or disposed report load cannot restore an old report or error', async () => {
  const element = { textContent: '', innerHTML: '' }; const delayed = deferred(); let signal
  const older = loadReport(element, async (url, options) => { signal = options.signal; return delayed.promise })
  const dispose = await loadReport(element, async () => ({ ok: true, json: async () => ({ state: 'unavailable', reason: 'Newest report unavailable' }) }))
  assert.equal(signal.aborted, true)
  delayed.resolve({ ok: false })
  await older
  assert.match(element.textContent, /Newest report unavailable/)
  dispose()
  assert.equal(element.innerHTML, '')
})

test('only completed upstream main scheduled/manual nightlies are trusted', () => {
  assert.equal(trustedRun(run(), 7), true)
  for (const override of [
    { head_repository: { full_name: 'fork/docfx' } }, { head_branch: 'feature' },
    { event: 'pull_request' }, { status: 'in_progress' }, { workflow_id: 8 }, { path: 'other.yml' },
  ]) assert.equal(trustedRun(run(override), 7), false)
})

test('selects failed-test nightly reports instead of filtering successful runs', async () => {
  const data = report(); data.results[1].outcome = 'incompatible'
  const result = await latestReport(adapter(), async id => { assert.equal(id, 99); return data })
  assert.equal(result.results[1].outcome, 'incompatible')
})

test('retained reports validate against their producing attempt after an unrelated job is retried', async () => {
  const data = report(); const original = JSON.stringify(data); const seen = []
  const api = async path => {
    seen.push(path)
    if (path === '/repos/dotnet/docfx/actions/runs/42/attempts/1') return run()
    return adapter([run({ run_attempt: 2 })])(path)
  }
  assert.equal(await latestReport(api, async () => data), data)
  assert.equal(JSON.stringify(data), original, 'Never relabel retained evidence as the newer attempt')
  assert.equal(seen.filter(path => path.includes('/attempts/')).length, 1)
})

test('retained reports reject invalid, future, absent or mismatched producing attempts', async () => {
  for (const value of ['0', '3', '-1', '1.5', '9007199254740992']) {
    const data = report(); data.source.runAttempt = value
    await assert.rejects(latestReport(async path => {
      assert.ok(!path.includes('/attempts/'), 'Reject invalid attempts before requesting an attempt record')
      return adapter([run({ run_attempt: 2 })])(path)
    }, async () => data))
  }
  for (const overrides of [
    { id: 43 }, { run_attempt: 2 }, { head_sha: 'c'.repeat(40) }, { workflow_id: 8 },
    { path: '.github/workflows/ci.yml' }, { head_repository: { full_name: 'fork/docfx' } },
    { head_branch: 'feature' }, { event: 'pull_request' }, { status: 'in_progress' },
  ]) {
    await assert.rejects(latestReport(async path => path.includes('/attempts/') ? run(overrides) : adapter([run({ run_attempt: 2 })])(path), async () => report()), /provenance|trusted run/)
  }
  await assert.rejects(latestReport(async path => {
    if (path.includes('/attempts/')) throw Object.assign(new Error('Attempt not found'), { status: 404 })
    return adapter([run({ run_attempt: 2 })])(path)
  }, async () => report()), { status: 404 })
})

test('paginates workflow runs and artifacts', async () => {
  const seen = []
  const api = async path => {
    seen.push(path)
    if (path.endsWith('/nightly.yml')) return { id: 7 }
    if (path.includes('/artifacts?')) return { artifacts: path.endsWith('page=1') ? Array.from({ length: 100 }, () => artifact({ name: 'unrelated' })) : [artifact()] }
    return { workflow_runs: path.endsWith('page=1') ? Array.from({ length: 100 }, () => run({ head_branch: 'other' })) : [run()] }
  }
  assert.equal((await latestReport(api, async () => report())).source.runId, '42')
  assert.equal(seen.filter(p => p.endsWith('page=2')).length, 2)
})

test('skips absent or expired artifacts without inventing a report', async () => {
  for (const artifacts of [[], [artifact({ expired: true })]]) {
    assert.equal((await latestReport(adapter(undefined, artifacts), async () => assert.fail('Must not download'))).state, 'unavailable')
  }
  assert.equal((await latestReport(adapter(), async () => { const error = new Error('Gone'); error.status = 410; throw error })).state, 'unavailable')
})

test('rejects mismatched artifact/run provenance, oversized and incomplete reports', async () => {
  for (const value of [artifact({ workflow_run: { id: 99 } }), artifact({ size_in_bytes: 999999999 })]) {
    await assert.rejects(latestReport(adapter(undefined, [value]), async () => report()))
  }
  const data = report(); data.results.pop()
  await assert.rejects(latestReport(adapter(), async () => data), /Incomplete/)
  await assert.rejects(latestReport(adapter(), async () => report(), [{ channel: '11.0', projectTfm: 'net10.0' }]), /reviewed SDK/)
})

test('resolves reviewed channels to exact SDKs, including previews and older project TFMs', () => {
  const config = { channels: [{ channel: '11.0', projectTfm: 'net10.0' }] }
  const result = resolveMatrix(config, { 'releases-index': [{ 'channel-version': '11.0', 'latest-sdk': '11.0.100-rc.1.123', 'support-phase': 'go-live' }] })
  assert.equal(result[0].sdk, '11.0.100-rc.1.123')
  assert.equal(result[0].projectTfm, 'net10.0')
  assert.throws(() => resolveMatrix(config, { 'releases-index': [] }), /No exact SDK/)
})

test('resolve CLI retains both SDK 11 project TFMs but installs the exact SDK only once', async () => {
  const temp = await mkdtemp(join(tmpdir(), 'docfx-matrix-'))
  try {
    const output = join(temp, 'matrix.json'); const githubOutput = join(temp, 'github-output')
    execFileSync(process.execPath, ['--input-type=module', '--eval', `
      import assert from 'node:assert/strict'
      import { main } from ${JSON.stringify(new URL('./report.mjs', import.meta.url).href)}
      let requests = 0
      globalThis.fetch = async url => {
        assert.equal(url, ${JSON.stringify(matrixConfig.releaseIndex)})
        requests++
        return { ok: true, json: async () => (${JSON.stringify(sdkIndex)}) }
      }
      await main(['resolve', ${JSON.stringify(output)}])
      assert.equal(requests, 1)
    `], { env: { ...process.env, GITHUB_OUTPUT: githubOutput } })
    const matrix = JSON.parse(await readFile(output, 'utf8'))
    assert.equal(matrix.length, 5)
    assert.deepEqual(matrix, resolveMatrix(matrixConfig, sdkIndex))
    const sdk11 = matrix.filter(t => t.channel === '11.0')
    assert.deepEqual(sdk11.map(t => t.projectTfm), ['net10.0', 'net11.0'])
    assert.ok(sdk11.every(t => t.sdk === '11.0.100-rc.1.123'))
    assert.equal(await readFile(githubOutput, 'utf8'), 'sdks<<SDK_LIST\n8.0.100\n9.0.100\n10.0.100\n11.0.100-rc.1.123\nSDK_LIST\n')
  } finally { await rm(temp, { recursive: true, force: true }) }
})

test('validates and renders 20 distinct cases in matrix order with net10.0 tool runtimes', () => {
  const data = report(resolveMatrix(matrixConfig, sdkIndex))
  assert.equal(data.results.length, 20)
  assert.equal(validateReport(data, data.source, now), data)
  const html = renderReport(data, now)
  const tables = [...html.matchAll(/<tbody>(.*?)<\/tbody>/gs)]
  assert.equal(tables.length, 1)
  const rows = [...tables[0][1].matchAll(/<tr>(.*?)<\/tr>/gs)].map(match => match[1])
  assert.equal(rows.length, 5)
  data.matrix.forEach((target, i) => {
    assert.ok(rows[i].includes(`<code>${target.sdk}</code><span>${target.projectTfm}</span>`))
    const indices = [...rows[i].matchAll(/data-case-index="(\d+)"/g)].map(match => Number(match[1]))
    assert.equal(indices.length, 4)
    assert.deepEqual(indices.map(index => [data.results[index].sdk, data.results[index].projectTfm]), Array(4).fill([target.sdk, target.projectTfm]))
    assert.doesNotMatch(rows[i], /href=/)
  })
  for (const name of ['Stable &amp; Basic', 'Stable &amp; Razor', 'Nightly &amp; Basic', 'Nightly &amp; Razor']) assert.match(html, new RegExp(`<strong>${name}</strong><span class="compatibility-tool">`))
  assert.equal((html.match(/2.80.1 &amp; net10.0<\/span>/g) ?? []).length, 2)
  assert.equal((html.match(/2.80.2-preview.1 &amp; net10.0<\/span>/g) ?? []).length, 2)
  assert.match(html, /<strong>20 passed<\/strong> · 0 incompatible<\/p>/)
})

test('validates and renders all 50 cases across four nightly tool targets', async () => {
  const data = multiToolReport()
  assert.equal(data.results.length, 50)
  assert.equal(validateReport(data, data.source, now), data)
  assert.equal(await latestReport(adapter(), async () => data, matrixConfig.channels), data)
  const html = renderReport(data, now)
  const rows = [...html.matchAll(/<tbody>(.*?)<\/tbody>/gs)][0][1].matchAll(/<tr>(.*?)<\/tr>/gs)
  for (const [i, row] of [...rows].entries()) {
    const indices = [...row[1].matchAll(/data-case-index="(\d+)"/g)].map(match => Number(match[1]))
    assert.equal(indices.length, 10)
    assert.ok(indices.every(index => data.results[index].sdk === data.matrix[i].sdk && data.results[index].projectTfm === data.matrix[i].projectTfm))
    assert.equal(new Set(indices.map(index => `${data.results[index].channel}/${data.results[index].toolFramework}/${data.results[index].scenario}`)).size, 10)
  }
  for (const { channel, framework } of data.toolTargets) {
    const version = channel === 'stable' ? '2.80.1' : '2.80.2-preview.1'
    assert.equal(html.split(`${version} &amp; ${framework}</span>`).length - 1, 2)
  }
  assert.equal((html.match(/data-case-index=/g) ?? []).length, 50)
})

test('requires each requested tool target and rejects fallback runtimes and mixed package bytes', () => {
  for (const framework of ['net8.0', 'net9.0', 'net10.0', 'net11.0']) {
    const complete = multiToolReport()
    const index = complete.results.findIndex(r => r.channel === 'nightly' && r.toolFramework === framework)
    for (const mutate of [
      r => r.results.splice(index, 1),
      r => { r.results[index] = r.results.find(row => row.channel === 'nightly' && row.toolFramework !== framework) },
      r => { r.results[index].toolRuntimeTfm = framework === 'net8.0' ? 'net10.0' : 'net8.0' },
      r => { r.results[index].packageSha256 = 'c'.repeat(64) },
      r => { r.results[index].toolVersion = '2.80.3-preview.1' },
    ]) {
      const data = structuredClone(complete); mutate(data)
      assert.throws(() => validateReport(data, undefined, now))
    }
  }
  for (const mutate of [
    r => { delete r.toolTargets }, r => r.toolTargets.push(r.toolTargets[0]),
    r => { r.toolTargets[0].channel = 'unknown' }, r => { r.toolTargets[0].framework = 'invalid' },
  ]) {
    const data = multiToolReport(); mutate(data)
    assert.throws(() => validateReport(data, undefined, now))
  }
})

test('trusted retrieval rejects an omitted nightly target while bounded local reports remain valid', async () => {
  const data = multiToolReport()
  data.toolTargets = data.toolTargets.filter(t => t.framework !== 'net9.0')
  data.results = data.results.filter(r => r.toolFramework !== 'net9.0')
  assert.equal(validateReport(data, undefined, now), data)
  await assert.rejects(latestReport(adapter(), async () => data, matrixConfig.channels), /reviewed tool framework matrix/)
})

test('failed tool installations retain their requested framework in the report and headers', () => {
  const data = multiToolReport()
  for (const row of data.results.filter(r => r.channel === 'nightly' && ['net8.0', 'net9.0'].includes(r.toolFramework))) {
    row.toolRuntimeTfm = null; row.toolVersion = null; row.packageSha256 = null; row.outcome = 'infrastructure-error'
  }
  assert.equal(validateReport(data, data.source, now), data)
  const html = renderReport(data, now)
  for (const framework of ['net8.0', 'net9.0']) assert.equal(html.split(`Not measured &amp; ${framework}</span>`).length - 1, 2)
  assert.equal((html.match(/data-case-index=/g) ?? []).length, 50)
  assert.equal((html.match(/data-outcome="infrastructure-error"/g) ?? []).length, 20)
})

test('each net11.0 observation is required and cannot be replaced with the SDK 11 net10.0 row', async () => {
  const complete = report(resolveMatrix(matrixConfig, sdkIndex))
  const indices = complete.results.flatMap((r, i) => r.projectTfm === 'net11.0' ? [i] : [])
  assert.equal(indices.length, 4)
  for (const index of indices) {
    const missing = structuredClone(complete)
    missing.results.splice(index, 1)
    assert.throws(() => validateReport(missing, missing.source, now), /Incomplete report/)
    await assert.rejects(latestReport(adapter(), async () => missing, matrixConfig.channels), /Incomplete report/)
    const replaced = structuredClone(complete)
    const row = replaced.results[index]
    replaced.results[index] = replaced.results.find(r => r.sdk === row.sdk && r.projectTfm === 'net10.0' && r.channel === row.channel && r.scenario === row.scenario)
    assert.equal(replaced.results.length, 20)
    assert.throws(() => validateReport(replaced, replaced.source, now), /Unexpected or duplicate result/)
    await assert.rejects(latestReport(adapter(), async () => replaced, matrixConfig.channels), /Unexpected or duplicate result/)
  }
})

test('trusted retrieval requires all five reviewed pairs while older 16-row evidence remains renderable', async () => {
  const data = report(resolveMatrix(matrixConfig, sdkIndex))
  assert.equal(await latestReport(adapter(), async () => data, matrixConfig.channels), data)
  data.matrix = data.matrix.filter(t => t.projectTfm !== 'net11.0')
  data.results = data.results.filter(r => r.projectTfm !== 'net11.0')
  assert.equal(data.results.length, 16)
  assert.equal(validateReport(data, undefined, now), data)
  assert.equal([...renderReport(data, now).matchAll(/data-outcome="passed"/g)].length, 16)
  await assert.rejects(latestReport(adapter(), async () => data, matrixConfig.channels), /reviewed SDK channel matrix/)
})

function harnessIdentifiers(contexts) {
  const path = fileURLToPath(new URL('Measure-Compatibility.ps1', import.meta.url)).replaceAll("'", "''")
  const matrix = JSON.stringify(resolveMatrix(matrixConfig, sdkIndex))
  // Evaluate the producer's assignments, not an independently reimplemented naming algorithm.
  return JSON.parse(execFileSync('pwsh', ['-NoProfile', '-NonInteractive', '-Command', `
    $ErrorActionPreference = 'Stop'
    $ast = [System.Management.Automation.Language.Parser]::ParseFile('${path}', [ref]$null, [ref]$null)
    $measurement = $ast.Find({ param($n) $n -is [System.Management.Automation.Language.AssignmentStatementAst] -and $n.Left.Extent.Text -eq '$measurementId' }, $true)
    $assignments = foreach ($name in @('id', 'logName', 'directory')) {
      $node = $ast.Find({ param($n) $n -is [System.Management.Automation.Language.AssignmentStatementAst] -and $n.Left.Extent.Text -eq ('$' + $name) }, $true)
      if (!$node) { throw "Missing harness assignment: $name" }
      $node.Extent.Text
    }
    $work = [IO.Path]::GetTempPath()
    $runs = foreach ($context in ('${JSON.stringify(contexts)}' | ConvertFrom-Json)) {
      $env:GITHUB_RUN_ID = $context.runId
      $env:GITHUB_RUN_ATTEMPT = $context.runAttempt
      if ($measurement) { Invoke-Expression $measurement.Extent.Text }
      $cases = foreach ($target in ('${matrix}' | ConvertFrom-Json)) {
        foreach ($channel in @('stable', 'nightly')) {
          $frameworks = if ($channel -eq 'nightly') { @('net8.0', 'net9.0', 'net10.0', 'net11.0') } else { @('net10.0') }
          foreach ($framework in $frameworks) {
            $tool = @{ channel = $channel; framework = $framework }
            foreach ($scenario in @('basic', 'razor')) {
              Invoke-Expression ($assignments -join [Environment]::NewLine)
              if (!$id.Contains($target.projectTfm) -or $directory -ne (Join-Path $work $id)) { throw 'Incorrect case identity' }
              if (!$id.Contains($framework)) { throw 'Missing tool framework in case identity' }
              @{ id = $id; log = $logName; directory = $directory }
            }
          }
        }
      }
      @{ cases = @($cases) }
    }
    ConvertTo-Json -InputObject @($runs) -Depth 5 -Compress
  `], { encoding: 'utf8', stdio: 'pipe' }))
}

test('actual harness identifiers isolate all 50 work directories and logs by tool and project TFM', () => {
  const [{ cases }] = harnessIdentifiers([{ runId: '42', runAttempt: '1' }])
  assert.equal(cases.length, 50)
  assert.equal(new Set(cases.map(row => row.directory)).size, 50)
  assert.equal(new Set(cases.map(row => row.log)).size, 50)
  for (const row of cases) assert.match(row.log, /^logs\/[a-zA-Z0-9.-]+\.log$/)
})

test('actual harness logs never reuse URLs across runs, retries or repeated local measurements', () => {
  const runs = harnessIdentifiers([
    { runId: '42', runAttempt: '1' }, { runId: '43', runAttempt: '1' },
    { runId: '42', runAttempt: '2' }, { runId: '42', runAttempt: '2' },
    { runId: null, runAttempt: null }, { runId: null, runAttempt: null },
  ])
  const urls = runs.flatMap(({ cases }) => cases.map(row => caseLogUrl(row, 'https://example.test/reports/report.json').href))
  assert.equal(new Set(urls).size, 300, 'Different measurements must not serve different evidence at the same log URL')
})

test('discovers an exact official stable release and rejects previews or malformed versions', () => {
  assert.equal(resolveStableRelease({ draft: false, prerelease: false, tag_name: 'v2.80.1' }).requestedVersion, '2.80.1')
  for (const release of [
    { draft: true, prerelease: false, tag_name: 'v2.80.1' },
    { draft: false, prerelease: true, tag_name: 'v2.81.0-preview.1' },
    { draft: false, prerelease: false, tag_name: 'latest' },
  ]) assert.throws(() => resolveStableRelease(release))
})

test('distinguishes explicit local versions and rejects silent fallback from latest stable', () => {
  const data = report()
  data.results[0].toolVersion = '2.78.5'
  assert.throws(() => validateReport(data, undefined, now), /requested stable release/)
  data.source = { repository: 'local', sha, runId: null, runAttempt: null, dirty: true }
  data.channels = ['stable']; data.results = data.results.filter(r => r.channel === 'stable')
  data.results.forEach(r => { r.toolVersion = '2.78.5' })
  data.stableSelection = { mode: 'explicit-version', requestedVersion: '2.78.5', reason: 'Newer release unavailable on the approved test feed.' }
  assert.match(renderReport(data, now), /not a measurement of the latest stable release/)
  assert.match(renderReport(data, now), /Current-main package not measured/)
  data.source = report().source
  assert.throws(() => validateReport(data, undefined, now), /local measurements only/)
  const unavailable = report()
  unavailable.results.filter(r => r.channel === 'stable').forEach(r => {
    r.outcome = 'infrastructure-error'; r.toolVersion = null; r.toolRuntimeTfm = null; r.packageSha256 = null
  })
  assert.match(renderReport(unavailable, now), /Requested latest stable release: <strong>2.80.1/)
})

test('exact installer refuses an older installed version and never retries a missing version', () => {
  const path = fileURLToPath(new URL('Measure-Compatibility.ps1', import.meta.url)).replaceAll("'", "''")
  const script = `
    $ErrorActionPreference = 'Stop'
    $ast = [System.Management.Automation.Language.Parser]::ParseFile('${path}', [ref]$null, [ref]$null)
    $definition = $ast.Find({ param($n) $n -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq 'Install-Tool' }, $false)
    Invoke-Expression $definition.Extent.Text
    $work = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString('N'))
    $logs = Join-Path $work 'logs'
    New-Item -ItemType Directory $logs | Out-Null
    $StableVersion = '2.80.1'; $StableVersionReason = 'Unit-test explicit request'
    $calls = [Collections.Generic.List[object]]::new()
    function Invoke-Logged($Arguments, $Log) { $calls.Add($Arguments); return @{ code = $script:exitCode; text = 'Synthetic installer boundary' } }
    try {
      $script:exitCode = 1
      $missing = Install-Tool 'stable' '' 'net10.0'
      if (!$missing.error -or $calls.Count -ne 1) { throw 'Missing package silently retried or passed' }
      if (($calls[0] -join ' ') -notmatch '--version 2.80.1') { throw 'Exact version was not requested' }
      New-Item -ItemType Directory (Join-Path $work 'stable-net10.0/.store/docfx/2.78.5') -Force | Out-Null
      $script:exitCode = 0
      $wrong = Install-Tool 'stable' '' 'net10.0'
      if ($wrong.error -notmatch 'differs from the exact requested version' -or $calls.Count -ne 2) { throw 'Wrong package identity accepted or retried' }
    } finally { Remove-Item $work -Recurse -Force }
  `
  execFileSync('pwsh', ['-NoProfile', '-Command', script], { stdio: 'pipe' })
})

test('reuses only current-main successful CI and distinguishes expiration from untested main', async () => {
  const ci = { ...run(), path: '.github/workflows/ci.yml', event: 'push', conclusion: 'success' }
  const api = (runs, artifacts) => async path => path.endsWith('/ci.yml') ? { id: 7 } : path.includes('/artifacts?') ? { artifacts } : { workflow_runs: runs }
  assert.deepEqual(await findSiteRun(api([ci], [artifact({ name: 'docs-site' })]), sha), { ready: true, runId: 42, artifactId: 99 })
  assert.deepEqual(await findSiteRun(api([ci], [artifact({ name: 'docs-site', expired: true })]), sha), { ready: true, runId: 42, artifactId: '' })
  for (const change of [{ event: 'pull_request' }, { head_sha: 'b'.repeat(40) }, { conclusion: 'failure' }, { head_repository: { full_name: 'fork/docfx' } }]) {
    assert.equal((await findSiteRun(api([{ ...ci, ...change }], []), sha)).ready, false)
  }
})

test('validation-only reports can describe a fork without becoming trusted website evidence', () => {
  const data = report()
  data.source.repository = 'vicancy/docfx'
  assert.deepEqual(data.channels, ['stable', 'nightly'])
  assert.equal(validateReport(data, undefined, now), data)
  assert.match(renderReport(data, now), /https:\/\/github.com\/vicancy\/docfx\/actions\/runs\/42/)
  assert.throws(() => validateReport(data, { ...data.source, repository: 'dotnet/docfx' }, now), /trusted run/)
  for (const repository of ['evil/../docfx', 'evil/docfx\" onclick=\"alert(1)', 'https://evil.example/docfx']) {
    data.source.repository = repository
    assert.throws(() => validateReport(data, undefined, now), /source/)
  }
})

test('validation-only or missing reports cannot authorize a nightly Pages refresh', async () => {
  const api = (artifacts, candidate = run()) => async path => path.endsWith('/nightly.yml') ? { id: 7 } : path.includes('/artifacts?') ? { artifacts } : candidate
  assert.equal(await productionReportReady(api([artifact()]), '42'), true)
  for (const artifacts of [[], [artifact({ name: 'sdk-compatibility-validation-v1' })], [artifact({ expired: true })]]) {
    assert.equal(await productionReportReady(api(artifacts), '42'), false)
  }
  for (const candidate of [run({ head_branch: 'validation' }), run({ head_repository: { full_name: 'vicancy/docfx' } }), run({ event: 'pull_request' })]) {
    assert.equal(await productionReportReady(api([artifact()], candidate), '42'), false)
  }
  await assert.rejects(productionReportReady(api([artifact({ workflow_run: { id: 41 } })]), '42'), /provenance/)
  await assert.rejects(productionReportReady(api([]), '../42'), /identity/)
  const paginated = async path => path.endsWith('/nightly.yml') ? { id: 7 } : !path.includes('/artifacts?') ? run() : {
    artifacts: path.endsWith('page=1') ? Array.from({ length: 100 }, () => artifact({ name: 'unrelated' })) : [artifact()],
  }
  assert.equal(await productionReportReady(paginated, '42'), true)
})

test('imports the CLI without process.argv[1], matching node --eval consumers', () => {
  execFileSync(process.execPath, ['--input-type=module', '--eval', `await import(${JSON.stringify(new URL('./report.mjs', import.meta.url).href)})`])
})

test('PowerShell harness and artifact reader parse without errors', () => {
  for (const file of ['Measure-Compatibility.ps1', 'Read-ReportArchive.ps1']) {
    const path = fileURLToPath(new URL(file, import.meta.url)).replaceAll("'", "''")
    execFileSync('pwsh', ['-NoProfile', '-Command', `$tokens=$null; $errors=$null; [System.Management.Automation.Language.Parser]::ParseFile('${path}',[ref]$tokens,[ref]$errors) > $null; if ($errors.Count) { throw ($errors -join '\n') }`])
  }
})

test('ZIP adapter reads only the bounded named JSON report without extracting other files', async () => {
  const temp = await mkdtemp(join(tmpdir(), 'docfx-report-test-'))
  try {
    const archive = join(temp, 'evidence.zip')
    const escaped = archive.replaceAll("'", "''")
    execFileSync('pwsh', ['-NoProfile', '-Command', `$z=[IO.Compression.ZipFile]::Open('${escaped}',[IO.Compression.ZipArchiveMode]::Create); foreach($name in @('compatibility-report.json','../unexpected.ps1')) { $w=[IO.StreamWriter]::new($z.CreateEntry($name).Open()); $w.Write('{}'); $w.Dispose() }; $z.Dispose()`])
    const script = fileURLToPath(new URL('./Read-ReportArchive.ps1', import.meta.url))
    assert.deepEqual(JSON.parse(execFileSync('pwsh', ['-NoProfile', '-File', script, '-ArchivePath', archive], { encoding: 'utf8' })), {})
    execFileSync('pwsh', ['-NoProfile', '-Command', `$z=[IO.Compression.ZipFile]::Open('${escaped}',[IO.Compression.ZipArchiveMode]::Update); $w=[IO.StreamWriter]::new($z.CreateEntry('compatibility-report.json').Open()); $w.Write('{}'); $w.Dispose(); $z.Dispose()`])
    assert.throws(() => execFileSync('pwsh', ['-NoProfile', '-File', script, '-ArchivePath', archive], { stdio: 'pipe' }))
  } finally { await rm(temp, { recursive: true, force: true }) }
})

async function makeArchive(path, entries) {
  const manifest = path + '.entries.json'
  await writeFile(manifest, JSON.stringify(entries.map(([name, bytes]) => ({ name, bytes: Buffer.from(bytes).toString('base64') }))))
  execFileSync('pwsh', ['-NoProfile', '-NonInteractive', '-Command', `
    $z = [IO.Compression.ZipFile]::Open('${path.replaceAll("'", "''")}', [IO.Compression.ZipArchiveMode]::Create)
    try {
      foreach ($entry in (Get-Content -Raw '${manifest.replaceAll("'", "''")}' | ConvertFrom-Json)) {
        $stream = $z.CreateEntry($entry.name).Open()
        try { $bytes = [Convert]::FromBase64String($entry.bytes); $stream.Write($bytes, 0, $bytes.Length) } finally { $stream.Dispose() }
      }
    } finally { $z.Dispose() }
  `], { stdio: 'pipe' })
}
const readArchive = (archive, evidence) => execFileSync('pwsh', ['-NoProfile', '-File', fileURLToPath(new URL('./Read-ReportArchive.ps1', import.meta.url)), '-ArchivePath', archive, ...(evidence ? ['-EvidenceDirectory', evidence] : [])], { encoding: 'utf8', stdio: 'pipe' })

test('archive and prepare export only named case logs and preserve JSON/log bytes', async () => {
  const temp = await mkdtemp(join(tmpdir(), 'docfx-evidence-test-'))
  try {
    const data = multiToolReport()
    const original = Buffer.from('\ufeff' + JSON.stringify(data, null, 2).replaceAll('\n', '\r\n') + '\r\n')
    const bytes = Buffer.from('\ufeffunit log\r\n\u001b[31mwarning XX123: unit text\u001b[0m\r\n')
    const archive = join(temp, 'evidence.zip'); const evidence = join(temp, 'evidence'); const output = join(temp, 'site', 'sdk-compatibility.json')
    await makeArchive(archive, [['compatibility-report.json', original], ...data.results.map(row => [row.log, bytes]), ['logs/install.log', 'not public'], ['logs/environment.log', 'not public'], ['../unexpected.ps1', 'not executed']])
    validateReport(JSON.parse(readArchive(archive)), data.source, now)
    readArchive(archive, evidence)
    await prepareReport(join(evidence, 'compatibility-report.json'), output)
    assert.deepEqual(await readFile(output), original)
    assert.deepEqual((await readdir(join(temp, 'site', 'logs'))).sort(), data.results.map(r => r.log.slice(5)).sort())
    for (const row of data.results) assert.deepEqual(await readFile(join(temp, 'site', row.log)), bytes)
    await assert.rejects(readFile(join(temp, 'unexpected.ps1')), { code: 'ENOENT' })
    await assert.rejects(prepareReport(join(evidence, 'compatibility-report.json'), join(evidence, 'copy.json')), /separate output directory/)
    const docs = JSON.parse((await readFile(new URL('../../docs/docfx.json', import.meta.url), 'utf8')).replace(/^\uFEFF/, ''))
    assert.ok(docs.build.resource.some(r => r.src === 'obj' && r.dest === 'reports' && r.files.includes('logs/*.log')))
  } finally { await rm(temp, { recursive: true, force: true }) }
})

test('archive, prepare and HTTP selection cannot mix logs across website replacements', async () => {
  const temp = await mkdtemp(join(tmpdir(), 'docfx-log-replacement-'))
  const site = join(temp, 'site'); const selections = []
  const server = createServer(async (request, response) => {
    const path = new URL(request.url, 'http://localhost').pathname
    if (path !== '/sdk-compatibility.json' && !/^\/logs\/[a-zA-Z0-9.-]+\.log$/.test(path)) { response.writeHead(404).end(); return }
    try { response.end(await readFile(join(site, path.slice(1)))) } catch (error) { response.writeHead(error.code === 'ENOENT' ? 404 : 500).end() }
  })
  try {
    await new Promise((resolve, reject) => { server.once('error', reject); server.listen(0, '127.0.0.1', resolve) })
    const base = new URL(`http://127.0.0.1:${server.address().port}/sdk-compatibility.json`)
    const identifiers = harnessIdentifiers([{ runId: '42', runAttempt: '1' }, { runId: '43', runAttempt: '1' }])
    const oldStates = []; const newStates = []; let older
    for (const [index, { cases }] of identifiers.entries()) {
      const data = report(resolveMatrix(matrixConfig, sdkIndex)); data.source.runId = String(42 + index)
      data.results.forEach((row, i) => { row.log = cases[i].log })
      const original = Buffer.from(JSON.stringify(data) + '\r\n')
      const log = Buffer.from(`Protocol fixture output from measurement ${index}\r\n`)
      const archive = join(temp, `${index}.zip`); const evidence = join(temp, `evidence-${index}`)
      await makeArchive(archive, [['compatibility-report.json', original], ...data.results.map(row => [row.log, log])])
      readArchive(archive, evidence)
      await prepareReport(join(evidence, 'compatibility-report.json'), join(site, 'sdk-compatibility.json'))
      assert.deepEqual(Buffer.from(await (await fetch(base)).arrayBuffer()), original)
      for (const row of data.results) assert.deepEqual(await readFile(join(site, row.log)), log)
      const selection = createCaseSelection(data, state => (index ? newStates : oldStates).push(state), undefined, base)
      selections.push(selection)
      await selection.select(0)
      assert.equal((index ? newStates : oldStates).at(-1).output, log.toString())
      if (index === 0) older = selection
    }
    await older.select(1)
    assert.equal(oldStates.at(-1).phase, 'error', 'An open old report must not display the replacement report\'s log')
    assert.match(oldStates.at(-1).error, /404/)
    assert.equal(oldStates.at(-1).row.outcome, 'passed')
    assert.equal(newStates.at(-1).phase, 'ready')
    assert.equal((await readdir(join(site, 'logs'))).length, 20)
  } finally {
    for (const selection of selections) selection.dispose()
    await new Promise(resolve => server.close(resolve))
    await rm(temp, { recursive: true, force: true })
  }
})

test('evidence export rejects duplicate, unsafe, case-colliding and oversized ZIP entries before extraction', async () => {
  const temp = await mkdtemp(join(tmpdir(), 'docfx-zip-guards-'))
  try {
    for (const [index, mutate] of [
      (data, entries) => entries.push([data.results[0].log, 'duplicate']),
      data => { data.results[0].log = '../outside.log' },
      data => { data.results[0].log = 'logs/Case.log'; data.results[1].log = 'logs/case.log' },
      (data, entries) => { entries[0][1] = Buffer.alloc(10 * 1024 * 1024 + 1) },
    ].entries()) {
      const data = report(); const entries = data.results.map(row => [row.log, 'unit log'])
      mutate(data, entries)
      const archive = join(temp, `${index}.zip`); const evidence = join(temp, `evidence-${index}`)
      await makeArchive(archive, [['compatibility-report.json', JSON.stringify(data)], ...entries])
      assert.throws(() => readArchive(archive, evidence))
      await assert.rejects(readdir(evidence), { code: 'ENOENT' })
    }
  } finally { await rm(temp, { recursive: true, force: true }) }
})

test('missing logs stay absent instead of exposing old evidence; local directory links are rejected', async () => {
  const temp = await mkdtemp(join(tmpdir(), 'docfx-missing-log-'))
  try {
    const data = report(); const archive = join(temp, 'evidence.zip'); const evidence = join(temp, 'evidence'); const output = join(temp, 'site', 'sdk-compatibility.json')
    await makeArchive(archive, [['compatibility-report.json', JSON.stringify(data)], ...data.results.slice(1).map(row => [row.log, 'unit output'])])
    readArchive(archive, evidence)
    await mkdir(join(temp, 'site', 'logs'), { recursive: true })
    await writeFile(join(temp, 'site', data.results[0].log), 'older run output')
    await prepareReport(join(evidence, 'compatibility-report.json'), output)
    assert.deepEqual(JSON.parse(await readFile(output, 'utf8')), data)
    await assert.rejects(readFile(join(temp, 'site', data.results[0].log)), { code: 'ENOENT' })
    await rm(join(evidence, 'logs'), { recursive: true })
    await symlink(join(temp, 'site', 'logs'), join(evidence, 'logs'), process.platform === 'win32' ? 'junction' : 'dir')
    await assert.rejects(prepareReport(join(evidence, 'compatibility-report.json'), output), /regular directory/)
  } finally { await rm(temp, { recursive: true, force: true }) }
})

test('trusted fetch exports the selected archive while temporary-branch evidence is never exported', async () => {
  const temp = await mkdtemp(join(tmpdir(), 'docfx-fetch-logs-'))
  const originalFetch = globalThis.fetch
  try {
    const data = report(resolveMatrix(matrixConfig, sdkIndex)); const archive = join(temp, 'evidence.zip'); const output = join(temp, 'site', 'sdk-compatibility.json')
    const original = Buffer.from(JSON.stringify(data) + '\r\n')
    await makeArchive(archive, [['compatibility-report.json', original], ...data.results.map(row => [row.log, 'unit log'])])
    const bytes = await readFile(archive)
    let branch = 'main'; let downloads = 0; let attempt = 1; let attemptRequests = 0
    globalThis.fetch = async url => {
      const path = new URL(url).pathname + new URL(url).search
      if (path.endsWith('/zip')) {
        downloads++
        return { ok: true, headers: new Headers(), body: (async function * () { yield bytes })() }
      }
      if (path === '/repos/dotnet/docfx/actions/runs/42/attempts/1') {
        attemptRequests++
        return { ok: true, json: async () => run() }
      }
      return { ok: true, json: async () => adapter([run({ head_branch: branch, run_attempt: attempt })])(path) }
    }
    for (attempt of [1, 2]) {
      await main(['fetch', output])
      assert.equal(downloads, attempt)
      assert.equal(attemptRequests, attempt - 1)
      assert.deepEqual(await readFile(output), original)
      assert.equal((await readdir(join(temp, 'site', 'logs'))).length, 20)
    }
    branch = 'temporary-validation'
    await main(['fetch', output])
    assert.equal(downloads, 2)
    assert.equal(JSON.parse(await readFile(output, 'utf8')).state, 'unavailable')
    await assert.rejects(readdir(join(temp, 'site', 'logs')), { code: 'ENOENT' })
  } finally { globalThis.fetch = originalFetch; await rm(temp, { recursive: true, force: true }) }
})

test('CLI prepares real schema content and rejects malformed input before writing', async () => {
  const temp = await mkdtemp(join(tmpdir(), 'docfx-report-cli-'))
  try {
    const input = join(temp, 'input.json'); const output = join(temp, 'site', 'output.json')
    await writeFile(input, JSON.stringify(report()))
    const cli = fileURLToPath(new URL('./report.mjs', import.meta.url))
    execFileSync(process.execPath, [cli, 'prepare', input, output])
    assert.equal(JSON.parse(await readFile(output, 'utf8')).source.sha, sha)
    await writeFile(input, '{}')
    assert.throws(() => execFileSync(process.execPath, [cli, 'prepare', input, output], { stdio: 'pipe' }))
  } finally { await rm(temp, { recursive: true, force: true }) }
})

function checkCli(path, flags = [], env = {}) {
  return spawnSync(process.execPath, [fileURLToPath(new URL('./report.mjs', import.meta.url)), 'check', path, ...flags], {
    encoding: 'utf8', env: { ...process.env, GITHUB_ACTIONS: '', GITHUB_STEP_SUMMARY: '', ...env },
  })
}

test('reporting check warns without failing incompatible observations; strict check fails', async () => {
  const temp = await mkdtemp(join(tmpdir(), 'docfx-check-policy-'))
  try {
    const input = join(temp, 'report.json')
    for (const outcome of ['passed', 'incompatible']) {
      const data = report(); data.results.at(-1).outcome = outcome
      const original = JSON.stringify(data)
      await writeFile(input, original)
      for (const strict of [false, true]) {
        const summary = join(temp, `${outcome}-${strict}.md`)
        const result = checkCli(input, strict ? ['--strict'] : [], { GITHUB_ACTIONS: 'true', GITHUB_STEP_SUMMARY: summary })
        assert.equal(result.status, strict && outcome === 'incompatible' ? 1 : 0, result.stderr)
        const markdown = await readFile(summary, 'utf8')
        assert.match(markdown, strict ? /Strict mode/ : /Reporting mode/)
        assert.ok(markdown.includes(`| nightly | 2.80.2-preview.1 | net10.0 | 10.0.401 | net10.0 | razor | ${outcome} |`))
        if (outcome === 'incompatible') {
          assert.match(result.stderr, /::warning title=SDK compatibility::1 incompatible result/)
          assert.match(result.stdout, /3 passed, 1 incompatible, 0 unavailable, 0 infrastructure-error/)
        } else {
          assert.doesNotMatch(result.stderr, /::warning/)
        }
        assert.equal(await readFile(input, 'utf8'), original)
      }
    }
    for (const flags of [['--strcit'], ['--strict', '--unknown']]) assert.equal(checkCli(input, flags).status, 1)
  } finally { await rm(temp, { recursive: true, force: true }) }
})

test('reporting and strict checks both fail unmeasured, invalid, missing or incomplete evidence', async () => {
  const temp = await mkdtemp(join(tmpdir(), 'docfx-check-failure-'))
  try {
    const input = join(temp, 'report.json')
    for (const outcome of ['infrastructure-error', 'unavailable']) {
      const data = report(); data.results[0].outcome = outcome
      await writeFile(input, JSON.stringify(data))
      for (const flags of [[], ['--strict']]) {
        const result = checkCli(input, flags, { GITHUB_ACTIONS: 'true' })
        assert.equal(result.status, 1)
        assert.match(result.stderr, /::error title=SDK compatibility::1 case\(s\) could not be measured/)
      }
    }
    for (const mutate of [r => r.results.pop(), r => { r.source.sha = 'wrong' }, r => { r.source.dirty = true }]) {
      const data = report(); mutate(data)
      await writeFile(input, JSON.stringify(data))
      for (const flags of [[], ['--strict']]) assert.equal(checkCli(input, flags).status, 1)
    }
    for (const malformed of ['{', '{}', JSON.stringify({ state: 'unavailable', reason: 'No report' })]) {
      await writeFile(input, malformed)
      for (const flags of [[], ['--strict']]) assert.equal(checkCli(input, flags).status, 1)
    }
    for (const flags of [[], ['--strict']]) assert.equal(checkCli(join(temp, 'missing.json'), flags).status, 1)
  } finally { await rm(temp, { recursive: true, force: true }) }
})

test('actual pwsh Actions wrapper uses report policy, not the last native child exit', async () => {
  const temp = await mkdtemp(join(tmpdir(), 'docfx-pwsh-exit-'))
  const literal = value => `'${value.replaceAll("'", "''")}'`
  try {
    const harness = fileURLToPath(new URL('./Measure-Compatibility.ps1', import.meta.url))
    // Execute the real post-finally statements, with protocol data only; no package measurements are simulated.
    const tail = execFileSync('pwsh', ['-NoProfile', '-NonInteractive', '-Command', `
      $ast = [System.Management.Automation.Language.Parser]::ParseFile(${literal(harness)}, [ref]$null, [ref]$null)
      $measurement = $ast.EndBlock.Statements | Where-Object { $_ -is [System.Management.Automation.Language.TryStatementAst] } | Select-Object -Last 1
      ($ast.EndBlock.Statements | Where-Object { $_.Extent.StartOffset -ge $measurement.Extent.EndOffset } | ForEach-Object { $_.Extent.Text }) -join [Environment]::NewLine
    `], { encoding: 'utf8' })
    assert.match(tail, /exit \$LASTEXITCODE/)
    const script = join(temp, 'finalize.ps1')
    const input = join(temp, 'compatibility-report.json')
    const prefix = nativeExit => `
      param([switch] $FailOnIncompatible)
      $ErrorActionPreference = 'Stop'
      $PSNativeCommandUseErrorActionPreference = $false
      $PSScriptRoot = ${literal(fileURLToPath(new URL('.', import.meta.url)))}
      $OutputDirectory = ${literal(temp)}
      & ${literal(process.execPath)} -e 'process.exit(${nativeExit})'
      if ($LASTEXITCODE -ne ${nativeExit}) { throw 'Native exit canary failed' }
    `
    const wrapper = strict => spawnSync('pwsh', ['-NoProfile', '-NonInteractive', '-Command', `
      $ErrorActionPreference = 'Stop'
      & ${literal(script)} ${strict ? '-FailOnIncompatible' : ''}
      if (Test-Path -LiteralPath variable:\\LASTEXITCODE) { exit $LASTEXITCODE }
    `], { encoding: 'utf8', env: { ...process.env, GITHUB_ACTIONS: '', GITHUB_STEP_SUMMARY: '' } })
    await writeFile(script, prefix(23))
    assert.equal(wrapper(false).status, 23, 'Canary must reproduce the Actions native-exit leak')
    const data = report(); data.results.at(-1).outcome = 'incompatible'
    await writeFile(input, JSON.stringify(data))
    await writeFile(script, prefix(23) + tail)
    const reporting = wrapper(false)
    assert.equal(reporting.status, 0, reporting.stderr)
    assert.match(reporting.stderr, /1 incompatible result/)
    assert.equal(wrapper(true).status, 1)
    data.results[0].outcome = 'infrastructure-error'
    await writeFile(input, JSON.stringify(data))
    await writeFile(script, prefix(0) + tail)
    assert.equal(wrapper(false).status, 1, 'A final native success must not hide an earlier infrastructure error')
    data.results.pop()
    await writeFile(input, JSON.stringify(data))
    assert.equal(wrapper(false).status, 1, 'Incomplete reports must still fail the wrapper')
    await writeFile(script, prefix(0) + "throw 'Unit-test harness error'\n" + tail)
    assert.equal(wrapper(false).status, 1, 'A script error must not reach successful finalization')
  } finally { await rm(temp, { recursive: true, force: true }) }
})

test('manual validation defaults safe and cannot reach package or Pages publication', async () => {
  const nightly = await readFile(new URL('../../.github/workflows/nightly.yml', import.meta.url), 'utf8')
  const docs = await readFile(new URL('../../.github/workflows/docs.yml', import.meta.url), 'utf8')
  assert.match(nightly, /validation_only:[\s\S]*?type: boolean\s+default: true/)
  const job = name => nightly.split(`  ${name}:`)[1].split(/\r?\n  [a-z][a-z-]*:\r?\n/)[0]
  const enabled = (name, github, inputs) => Function('github', 'inputs', `return (${job(name).match(/^    if: (.+)/m)[1].trim()})`)(github, inputs)
  for (const repository of ['dotnet/docfx', 'vicancy/docfx']) {
    for (const ref of ['refs/heads/main', 'refs/heads/validation']) {
      const github = { repository, ref, event_name: 'workflow_dispatch' }
      const inputs = { validation_only: true }
      for (const name of ['build-nightly-package', 'test-nightly-package', 'sdk-compatibility']) assert.equal(enabled(name, github, inputs), true)
      assert.equal(enabled('publish-github-packages', github, inputs), false)
    }
  }
  for (const event_name of ['schedule', 'workflow_dispatch']) {
    assert.equal(enabled('publish-github-packages', { repository: 'dotnet/docfx', ref: 'refs/heads/main', event_name }, { validation_only: false }), true)
    assert.equal(enabled('publish-github-packages', { repository: 'vicancy/docfx', ref: 'refs/heads/main', event_name }, { validation_only: false }), false)
    assert.equal(enabled('publish-github-packages', { repository: 'dotnet/docfx', ref: 'refs/heads/validation', event_name }, { validation_only: false }), false)
  }
  const build = job('build-nightly-package')
  assert.doesNotMatch(build, /packages: write|nuget push/)
  for (const tfm of ['net8.0', 'net9.0', 'net10.0']) assert.ok(build.includes(`dotnet test -c Release -f ${tfm} --no-build`))
  assert.match(build, /dotnet pack/)
  assert.match(build, /name: nightly-tool-package/)
  for (const name of ['test-nightly-package', 'sdk-compatibility']) {
    assert.match(job(name), /needs: \[build-nightly-package\]/)
    assert.match(job(name), /name: nightly-tool-package/)
    assert.match(job(name), /needs.build-nightly-package.outputs.version/)
    assert.doesNotMatch(job(name), /packages: write|pages: write/)
  }
  assert.doesNotMatch(job('sdk-compatibility'), /SkipStable|FailOnIncompatible|VALIDATION_ONLY|@options/)
  assert.match(job('sdk-compatibility'), /Measure-Compatibility.ps1 -MatrixPath drop\/compatibility-matrix.json/)
  assert.match(job('test-nightly-package'), /docfx metadata\s+docfx build\s+docfx pdf/)
  assert.deepEqual(matrixConfig.channels.map(t => `${t.channel}/${t.projectTfm}`), ['8.0/net8.0', '9.0/net9.0', '10.0/net10.0', '11.0/net10.0', '11.0/net11.0'])
  assert.equal(matrixConfig.channels.length * report().channels.length * report().scenarios.length, 20)
  assert.match(job('sdk-compatibility'), /dotnet-version: \$\{\{ steps.matrix.outputs.sdks \}\}/)
  assert.match(nightly, /inputs.validation_only && 'sdk-compatibility-validation-v1' \|\| 'sdk-compatibility-v1'/)
  assert.match(docs, /report.mjs production-ready/)
  assert.match(docs, /if: github.event.workflow_run.name == 'ci' \|\| steps.production-report.outputs.ready == 'true'\s+id: site-run/)
  assert.match(docs, /ready: \$\{\{ steps.site-run.outputs.ready \}\}/)
  assert.match(docs, /if: needs.site.outputs.ready == 'true'/)
})

test('CI measures the exact distribution package in a job isolated from snapshot tests', async () => {
  const ci = await readFile(new URL('../../.github/workflows/ci.yml', import.meta.url), 'utf8')
  const packageJob = ci.replaceAll('\r\n', '\n').split('\n  package:\n')[1]?.split(/\n  [\w-]+:\n/)[0]
  assert.ok(packageJob, 'Distribution measurements need an isolated job')
  assert.match(packageJob, /uses: actions\/checkout@v7/)
  assert.doesNotMatch(packageJob, /dotnet test|percy exec|build-docs|download-artifact/)
  const stages = [
    'uses: ./.github/actions/build\n',
    'name: Test compatibility reporting',
    'name: Pack tool for distribution tests',
    'name: Publish tool for distribution tests',
    'name: Repack tool without rebuilding',
    'name: Verify distributed templates',
    'name: Packaged SDK compatibility smoke',
    'name: Upload compatibility smoke evidence',
  ]
  let previous = -1
  for (const stage of stages) {
    const index = packageJob.indexOf(stage)
    assert.ok(index > previous, `Missing or misplaced CI stage: ${stage}`)
    previous = index
  }
  const packaging = packageJob.match(/run: dotnet (?:pack|publish) src\/docfx[^\r\n]*/g) ?? []
  assert.equal(packaging.length, 3)
  for (const command of packaging) assert.match(command, /\/p:BaseOutputPath=bin\/package-test\//)
  assert.doesNotMatch(ci, /git (?:clean|reset|restore)|BUILD_SERVER:/)
})

test('CI cleans coverage downloads without weakening the harness source-state check', async () => {
  const ci = await readFile(new URL('../../.github/workflows/ci.yml', import.meta.url), 'utf8')
  const codecov = ci.split('- uses: codecov/codecov-action@v7')[1].split(/\r?\n    - /)[0]
  assert.match(codecov, /\n        cleanup: true\s/)
  assert.match(codecov, /fail_ci_if_error: false/)
  const temp = await mkdtemp(join(tmpdir(), 'docfx-source-state-'))
  try {
    execFileSync('git', ['init', '--quiet', temp])
    const harness = fileURLToPath(new URL('./Measure-Compatibility.ps1', import.meta.url)).replaceAll("'", "''")
    const dirty = () => execFileSync('pwsh', ['-NoProfile', '-NonInteractive', '-Command', `
      $ast = [System.Management.Automation.Language.Parser]::ParseFile('${harness}', [ref]$null, [ref]$null)
      $assignment = $ast.Find({ param($n) $n -is [System.Management.Automation.Language.AssignmentStatementAst] -and $n.Left.Extent.Text -eq '$dirty' }, $true)
      Invoke-Expression $assignment.Extent.Text
      ConvertTo-Json $dirty
    `], { cwd: temp, encoding: 'utf8', stdio: 'pipe' }).trim()
    assert.equal(dirty(), 'false')
    const downloads = ['codecov', 'codecov.SHA256SUM', 'codecov.SHA256SUM.sig']
    for (const name of downloads) await writeFile(join(temp, name), 'Inert test input; not an executable')
    assert.equal(dirty(), 'true', 'Reproduce the coverage download/source-provenance collision')
    for (const name of downloads) await rm(join(temp, name))
    assert.equal(dirty(), 'false')
    await writeFile(join(temp, 'Unexpected.cs'), '// Untracked source must still invalidate CI provenance')
    assert.equal(dirty(), 'true')
  } finally { await rm(temp, { recursive: true, force: true }) }
})

test('dirty source diagnostics preserve exact paths without changing the source guard', async () => {
  const temp = await mkdtemp(join(tmpdir(), 'docfx-source-diagnostics-'))
  try {
    const repository = join(temp, 'repository')
    const logs = join(temp, 'logs')
    await mkdir(repository)
    await mkdir(logs)
    execFileSync('git', ['init', '--quiet', repository])
    const harness = fileURLToPath(new URL('./Measure-Compatibility.ps1', import.meta.url)).replaceAll("'", "''")
    const diagnose = () => execFileSync('pwsh', ['-NoProfile', '-NonInteractive', '-Command', `
      $ErrorActionPreference = 'Stop'
      $ast = [System.Management.Automation.Language.Parser]::ParseFile('${harness}', [ref]$null, [ref]$null)
      $assignment = $ast.Find({ param($n) $n -is [System.Management.Automation.Language.AssignmentStatementAst] -and $n.Left.Extent.Text -eq '$dirty' }, $true)
      $diagnostic = $ast.Find({ param($n) $n -is [System.Management.Automation.Language.IfStatementAst] -and $n.Extent.Text.StartsWith('if ($dirty)') }, $true)
      if (-not $diagnostic) { throw 'Missing dirty source diagnostics' }
      $logs = '${logs.replaceAll("'", "''")}'
      Invoke-Expression $assignment.Extent.Text
      Invoke-Expression $diagnostic.Extent.Text
      ConvertTo-Json $dirty
    `], { cwd: repository, encoding: 'utf8', stdio: 'pipe' })
    assert.equal(diagnose().trim(), 'false')
    assert.deepEqual(await readdir(logs), [])
    await mkdir(join(repository, 'unexpected'))
    await writeFile(join(repository, 'unexpected', 'input.cs'), '// Must remain dirty')
    const expected = execFileSync('git', ['status', '--porcelain=v1', '--untracked-files=all'], { cwd: repository, encoding: 'utf8' }).trim()
    const output = diagnose()
    assert.match(output, /Source tree is dirty:/)
    assert.ok(output.includes(expected))
    assert.match(output, /true\s*$/)
    assert.equal((await readFile(join(logs, 'source-state.log'), 'utf8')).trim(), expected)
    assert.equal(execFileSync('git', ['status', '--porcelain=v1', '--untracked-files=all'], { cwd: repository, encoding: 'utf8' }).trim(), expected)
  } finally { await rm(temp, { recursive: true, force: true }) }
})

test('workflow contract keeps exact packages, bounded PR smoke, failure evidence and one publisher', async () => {
  const read = path => readFile(new URL(`../../${path}`, import.meta.url), 'utf8')
  const [ci, nightly, docs, site] = await Promise.all(['.github/workflows/ci.yml', '.github/workflows/nightly.yml', '.github/workflows/docs.yml', '.github/actions/build-docs/action.yml'].map(read))
  assert.match(ci, /-SkipStable -FailOnIncompatible/)
  assert.match(ci, /node --test test\/compatibility\/report.test.mjs/)
  assert.match(ci, /name: docs-site/)
  assert.match(nightly, /--version \$env:TOOL_VERSION/)
  assert.doesNotMatch(nightly, /tool install.*--prerelease/)
  assert.ok(nightly.indexOf('Measure and check latest stable') < nightly.indexOf('Upload compatibility evidence'))
  assert.doesNotMatch(nightly, /report.mjs check/)
  assert.match(nightly, /name: Upload compatibility evidence[^\r\n]*\s+if: always\(\)/)
  assert.match(nightly, /path: drop\/compatibility-report\s+if-no-files-found: error/)
  assert.equal((nightly.match(/continue-on-error:/g) ?? []).length, 1)
  assert.match(nightly, /uses: actions\/setup-dotnet@v5\s+continue-on-error: true/)
  const harness = await readFile(new URL('./Measure-Compatibility.ps1', import.meta.url), 'utf8')
  assert.match(harness, /& node \(Join-Path \$PSScriptRoot 'report.mjs'\) @checkArguments\s+exit \$LASTEXITCODE/)
  assert.match(harness, /if \(\$FailOnIncompatible\) \{ \$checkArguments \+= '--strict' \}/)
  assert.match(harness, /'build', '--no-restore'[^\r\n]*\s+if \(\$result.code -ne 0\) \{ throw 'Fixture build failed; compatibility was not measured.' \}/)
  assert.match(nightly, /retention-days: 14/)
  assert.match(docs, /workflows: \[ci, nightly\]/)
  assert.match(docs, /ref: main/)
  assert.match(docs, /cancel-in-progress: false/)
  assert.match(docs, /report.mjs fetch docs\/_site\/reports\/sdk-compatibility.json/)
  assert.doesNotMatch(docs, /Measure-Compatibility/)
  assert.doesNotMatch(ci + nightly, /actions\/deploy-pages/)
  assert.equal((docs.match(/actions\/deploy-pages/g) ?? []).length, 1)
  assert.match(site, /samples\/seed\/docfx.json --output docs\/_site\/seed/)
})
