// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import test from 'node:test'
import assert from 'node:assert/strict'
import { readFile, mkdtemp, writeFile, rm } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { execFileSync, spawnSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'
import { latestReport, resolveMatrix, resolveStableRelease, trustedRun, findSiteRun, productionReportReady } from './report.mjs'
import { validateReport, renderReport, loadReport } from '../../docs/template/public/sdk-compatibility.mjs'

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

test('renders incompatible findings and unmeasured cases as warnings, never passing evidence', () => {
  const data = report()
  assert.doesNotMatch(renderReport(data, now), /Compatibility issues found|could not be measured/)
  data.results[1].outcome = 'incompatible'
  data.results[2].outcome = 'infrastructure-error'
  data.results[3].outcome = 'unavailable'
  const html = renderReport(data, now)
  assert.match(html, /alert-warning.*Compatibility issues found: 1 incompatible result/)
  assert.match(html, /A completed report does not mean all combinations passed/)
  assert.match(html, /alert-danger.*2 case\(s\) could not be measured/)
  assert.equal((html.match(/<strong>passed<\/strong>/g) ?? []).length, 1)
  assert.equal((html.match(/<strong>incompatible<\/strong>/g) ?? []).length, 1)
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
  assert.match(renderReport(data, now), /Latest recorded evidence/)
  data.generatedAt = '2099-01-01T00:00:00Z'
  assert.throws(() => validateReport(data, undefined, now), /timestamp/)
  data.generatedAt = 'invalid'
  assert.throws(() => validateReport(data, undefined, now), /timestamp/)
})

test('HTML-escapes report content and never creates artifact-controlled URLs', () => {
  const data = report()
  data.results[0].diagnostics = '<script>alert(1)</script>'
  data.results[0].os = '<img src=x onerror=alert(1)>'
  const html = renderReport(data, now)
  assert.ok(!html.includes('<script>'))
  assert.ok(!html.includes('<img'))
  assert.match(html, /&lt;script&gt;/)
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
  const tables = [...renderReport(data, now).matchAll(/<tbody>(.*?)<\/tbody>/gs)]
  assert.equal(tables.length, 2)
  for (const [index, channel] of ['nightly', 'stable'].entries()) {
    const rows = [...tables[index][1].matchAll(/<tr>(.*?)<\/tr>/gs)].map(match => match[1])
    const expected = data.results.filter(r => r.channel === channel)
    assert.equal(rows.length, 10)
    expected.forEach((row, i) => {
      assert.ok(rows[i].includes(`<code>${row.sdk}</code><br>${row.projectTfm}`))
      assert.ok(rows[i].includes(`<td>${row.toolVersion}<br>net10.0</td><td>${row.scenario}</td>`))
      assert.ok(rows[i].includes(`<code>${row.log}</code>`))
    })
  }
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
  assert.equal([...renderReport(data, now).matchAll(/<strong>passed<\/strong>/g)].length, 16)
  await assert.rejects(latestReport(adapter(), async () => data, matrixConfig.channels), /reviewed SDK channel matrix/)
})

test('actual harness identifiers isolate all 20 work directories and log files by project TFM', () => {
  const path = fileURLToPath(new URL('Measure-Compatibility.ps1', import.meta.url)).replaceAll("'", "''")
  const matrix = JSON.stringify(resolveMatrix(matrixConfig, sdkIndex))
  // Evaluate only the real identifier assignments, never simulate fixture execution or measurements.
  execFileSync('pwsh', ['-NoProfile', '-NonInteractive', '-Command', `
    $ErrorActionPreference = 'Stop'
    $ast = [System.Management.Automation.Language.Parser]::ParseFile('${path}', [ref]$null, [ref]$null)
    $assignments = foreach ($name in @('id', 'logName', 'directory')) {
      $node = $ast.Find({ param($n) $n -is [System.Management.Automation.Language.AssignmentStatementAst] -and $n.Left.Extent.Text -eq ('$' + $name) }, $true)
      if (!$node) { throw "Missing harness assignment: $name" }
      $node.Extent.Text
    }
    $work = [IO.Path]::GetTempPath()
    $directories = [Collections.Generic.HashSet[string]]::new()
    $logs = [Collections.Generic.HashSet[string]]::new()
    foreach ($target in ('${matrix}' | ConvertFrom-Json)) {
      foreach ($channel in @('stable', 'nightly')) {
        $tool = @{ channel = $channel }
        foreach ($scenario in @('basic', 'razor')) {
          Invoke-Expression ($assignments -join [Environment]::NewLine)
          if (!$id.Contains($target.projectTfm) -or $logName -ne "logs/$id.log" -or $directory -ne (Join-Path $work $id)) { throw 'Incorrect case identity' }
          if (!$directories.Add($directory) -or !$logs.Add($logName)) { throw 'SDK/TFM case collision' }
        }
      }
    }
    if ($directories.Count -ne 20 -or $logs.Count -ne 20) { throw 'Expected 20 isolated cases' }
  `], { stdio: 'pipe' })
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
  assert.match(renderReport(unavailable, now), /Requested latest stable release at measurement time: <strong>2.80.1/)
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
      $missing = Install-Tool 'stable' ''
      if (!$missing.error -or $calls.Count -ne 1) { throw 'Missing package silently retried or passed' }
      if (($calls[0] -join ' ') -notmatch '--version 2.80.1') { throw 'Exact version was not requested' }
      New-Item -ItemType Directory (Join-Path $work 'stable/.store/docfx/2.78.5') -Force | Out-Null
      $script:exitCode = 0
      $wrong = Install-Tool 'stable' ''
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

test('CLI prepares real schema content and rejects malformed input before writing', async () => {
  const temp = await mkdtemp(join(tmpdir(), 'docfx-report-cli-'))
  try {
    const input = join(temp, 'input.json'); const output = join(temp, 'output.json')
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
        assert.ok(markdown.includes(`| nightly | 2.80.2-preview.1 | 10.0.401 | net10.0 | razor | ${outcome} |`))
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
