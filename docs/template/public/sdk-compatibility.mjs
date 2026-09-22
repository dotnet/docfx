// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export const staleAfterDays = 7
const sdkPattern = /^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$/
const tfmPattern = /^net\d+\.0$/
const outcomes = ['passed', 'incompatible', 'unavailable', 'infrastructure-error']
const assert = (value, message) => { if (!value) throw new Error(message) }
const text = (value, max = 2000) => typeof value === 'string' && value.length > 0 && value.length <= max
const date = value => typeof value === 'string' && /^\d{4}-\d\d-\d\dT/.test(value) && Number.isFinite(Date.parse(value))

export function validateReport(report, expected, now = Date.now()) {
  assert(report?.schemaVersion === 1, 'Unsupported report schema.')
  assert(date(report.generatedAt) && Date.parse(report.generatedAt) <= now + 300000, 'Invalid report timestamp.')
  const source = report.source
  assert(source && (source.repository === 'local' || /^[a-zA-Z0-9][a-zA-Z0-9-]{0,38}\/[a-zA-Z0-9][a-zA-Z0-9_.-]{0,99}$/.test(source.repository)) && /^[a-f0-9]{40}$/.test(source.sha), 'Invalid report source.')
  assert(typeof source.dirty === 'boolean', 'Missing source working-tree state.')
  assert(source.repository === 'local' ? source.runId === null && source.runAttempt === null : /^\d+$/.test(source.runId) && /^\d+$/.test(source.runAttempt) && !source.dirty, 'Invalid run provenance.')
  if (expected) {
    for (const key of ['repository', 'sha', 'runId', 'runAttempt']) {
      assert(String(source[key]) === String(expected[key]), `Report ${key} differs from the trusted run.`)
    }
    assert(JSON.stringify([...report.channels].sort()) === '["nightly","stable"]', 'Nightly report must contain both package channels.')
  }
  assert(Array.isArray(report.matrix) && report.matrix.length > 0 && report.matrix.length <= 12, 'Invalid matrix.')
  const targets = report.matrix.map(target => {
    assert(sdkPattern.test(target.sdk) && tfmPattern.test(target.projectTfm), 'Invalid matrix target.')
    return `${target.sdk}/${target.projectTfm}`
  })
  assert(new Set(targets).size === targets.length, 'Duplicate matrix target.')
  assert(Array.isArray(report.channels) && report.channels.length > 0 && report.channels.length <= 2 && report.channels.every(c => ['stable', 'nightly'].includes(c)) && new Set(report.channels).size === report.channels.length, 'Invalid package channels.')
  if (report.channels.includes('stable')) {
    const selection = report.stableSelection
    assert(selection && ['latest-release', 'explicit-version'].includes(selection.mode) && text(selection.reason), 'Invalid stable release selection.')
    assert(selection.requestedVersion === null || /^\d+\.\d+\.\d+$/.test(selection.requestedVersion), 'Invalid requested stable version.')
    if (selection.mode === 'explicit-version') assert(source.repository === 'local' && selection.requestedVersion, 'Explicit versions are local measurements only.')
    if (expected) assert(selection.mode === 'latest-release', 'Nightly must resolve the latest stable release.')
  } else { assert(report.stableSelection === null, 'Unexpected stable release selection.') }
  assert(JSON.stringify(report.scenarios) === '["basic","razor"]', 'Missing fixture scenarios.')
  const wanted = new Set(targets.flatMap(t => report.channels.flatMap(c => report.scenarios.map(s => `${t}/${c}/${s}`))))
  assert(Array.isArray(report.results) && report.results.length === wanted.size, 'Incomplete report.')
  const packageIdentities = new Map()
  for (const row of report.results) {
    const key = `${row.sdk}/${row.projectTfm}/${row.channel}/${row.scenario}`
    assert(wanted.delete(key), 'Unexpected or duplicate result.')
    assert(outcomes.includes(row.outcome), 'Unknown outcome.')
    assert(text(row.os, 300) && text(row.diagnostics) && /^logs\/[a-zA-Z0-9.-]+\.log$/.test(row.log), 'Invalid diagnostic evidence.')
    assert(date(row.testedAt) && Date.parse(row.testedAt) <= Date.parse(report.generatedAt), 'Invalid test timestamp.')
    assert(row.selectedSdk === null || row.selectedSdk === row.sdk, 'Selected SDK differs from requested SDK.')
    assert(row.toolVersion === null || sdkPattern.test(row.toolVersion), 'Invalid package version.')
    assert(row.toolRuntimeTfm === null || tfmPattern.test(row.toolRuntimeTfm), 'Invalid tool runtime TFM.')
    assert(row.packageSha256 === null || /^[a-f0-9]{64}$/.test(row.packageSha256), 'Invalid package hash.')
    if (['passed', 'incompatible'].includes(row.outcome)) {
      assert(row.selectedSdk === row.sdk && row.toolVersion && row.toolRuntimeTfm && row.packageSha256, 'Measured result is missing SDK/package identity.')
      if (row.channel === 'stable') assert(row.toolVersion === report.stableSelection.requestedVersion, 'Measured package differs from the requested stable release.')
    }
    if (row.channel === 'stable' && row.toolVersion) assert(!row.toolVersion.includes('-'), 'Stable channel contains a prerelease.')
    if (row.packageSha256) {
      const identity = `${row.toolVersion}/${row.toolRuntimeTfm}/${row.packageSha256}`
      assert(!packageIdentities.has(row.channel) || packageIdentities.get(row.channel) === identity, 'Package identity changes within a channel.')
      packageIdentities.set(row.channel, identity)
    }
  }
  return report
}

const escape = value => String(value ?? 'Not measured').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c])

export function renderReport(report, now = Date.now()) {
  validateReport(report, undefined, now)
  const stale = now - Date.parse(report.generatedAt) > staleAfterDays * 86400000
  const source = report.source
  const runUrl = source.repository === 'local' ? null : `https://github.com/${source.repository}/actions/runs/${source.runId}/attempts/${source.runAttempt}`
  let html = `<p class="alert ${stale ? 'alert-warning' : 'alert-info'}"><strong>${stale ? 'Stale evidence' : 'Latest recorded evidence'}</strong> — measured ${escape(report.generatedAt)}. Evidence becomes stale after ${staleAfterDays} days; this is not a support guarantee.</p>`
  html += `<p>${runUrl ? `<a href="${runUrl}">Workflow run and downloadable logs</a>` : 'Local measurement (not a published nightly run)'}. <a href="../reports/sdk-compatibility.json">Download report JSON</a>. Source: <code>${escape(source.sha)}</code>${source.dirty ? ' (with local uncommitted changes)' : ''}.</p>`
  if (!report.channels.includes('nightly')) html += '<p class="alert alert-warning"><strong>Current-main package not measured in this report.</strong> Released-package results do not validate current main.</p>'
  for (const channel of ['nightly', 'stable'].filter(channel => report.channels.includes(channel))) {
    html += `<h3>${channel === 'stable' ? 'Released DocFX' : 'Nightly DocFX'}</h3>`
    if (channel === 'stable') {
      const selection = report.stableSelection
      html += selection.mode === 'explicit-version'
        ? `<p class="alert alert-warning"><strong>Explicit local version: ${escape(selection.requestedVersion)}</strong> — not a measurement of the latest stable release. ${escape(selection.reason)}</p>`
        : `<p>Requested latest stable release at measurement time: <strong>${escape(selection.requestedVersion)}</strong>. An unavailable exact version is an infrastructure error, never an older-version fallback.</p>`
    }
    html += '<div class="table-responsive"><table class="table"><thead><tr><th>Project SDK / TFM</th><th>DocFX / runtime TFM</th><th>Scenario</th><th>Result</th></tr></thead><tbody>'
    for (const row of report.results.filter(r => r.channel === channel)) {
      html += `<tr><td><code>${escape(row.sdk)}</code><br>${escape(row.projectTfm)}</td><td>${escape(row.toolVersion)}<br>${escape(row.toolRuntimeTfm)}</td><td>${escape(row.scenario)}</td><td><strong>${escape(row.outcome)}</strong><details><summary>Evidence</summary><p>${escape(row.diagnostics)}</p><p>Selected SDK: ${escape(row.selectedSdk)}<br>OS: ${escape(row.os)}<br>Tested: ${escape(row.testedAt)}<br>Log in artifact: <code>${escape(row.log)}</code><br>Package SHA-256: <code>${escape(row.packageSha256)}</code></p></details></td></tr>`
    }
    html += '</tbody></table></div>'
  }
  return html
}

export async function loadReport(element, fetchReport = fetch) {
  try {
    const response = await fetchReport(new URL('../reports/sdk-compatibility.json', import.meta.url))
    if (!response.ok) throw new Error('No compatibility report is available in this site build.')
    const data = await response.json()
    if (data.state === 'unavailable') throw new Error(text(data.reason) ? data.reason : 'Compatibility evidence is unavailable.')
    element.innerHTML = renderReport(data)
  } catch (error) {
    element.textContent = `Compatibility evidence unavailable. ${error.message}`
  }
}

if (typeof document !== 'undefined') {
  const element = document.getElementById('sdk-compatibility-report')
  if (element) await loadReport(element)
}
