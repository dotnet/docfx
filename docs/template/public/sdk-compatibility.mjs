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
  assert([1, 2].includes(report?.schemaVersion), 'Unsupported report schema.')
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
  const multipleTools = report.schemaVersion === 2
  const toolTargets = multipleTools ? report.toolTargets : report.channels.map(channel => ({ channel, framework: null }))
  assert(Array.isArray(toolTargets) && toolTargets.length > 0 && toolTargets.length <= 8, 'Invalid tool targets.')
  const toolKeys = toolTargets.map(target => {
    assert(target && report.channels.includes(target.channel) && (!multipleTools || tfmPattern.test(target.framework)), 'Invalid tool target.')
    return multipleTools ? `${target.channel}/${target.framework}` : target.channel
  })
  assert(new Set(toolKeys).size === toolKeys.length && report.channels.every(channel => toolTargets.some(t => t.channel === channel)), 'Duplicate or missing tool target.')
  const wanted = new Set(targets.flatMap(t => toolKeys.flatMap(tool => report.scenarios.map(s => `${t}/${tool}/${s}`))))
  assert(Array.isArray(report.results) && report.results.length === wanted.size, 'Incomplete report.')
  const packageIdentities = new Map()
  for (const row of report.results) {
    const toolKey = multipleTools ? `${row.channel}/${row.toolFramework}` : row.channel
    const key = `${row.sdk}/${row.projectTfm}/${toolKey}/${row.scenario}`
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
      if (multipleTools) assert(row.toolRuntimeTfm === row.toolFramework, 'Measured tool runtime differs from the requested framework.')
      if (row.channel === 'stable') assert(row.toolVersion === report.stableSelection.requestedVersion, 'Measured package differs from the requested stable release.')
    }
    if (row.channel === 'stable' && row.toolVersion) assert(!row.toolVersion.includes('-'), 'Stable channel contains a prerelease.')
    if (row.packageSha256) {
      const identity = multipleTools ? `${row.toolVersion}/${row.packageSha256}` : `${row.toolVersion}/${row.toolRuntimeTfm}/${row.packageSha256}`
      assert(!packageIdentities.has(row.channel) || packageIdentities.get(row.channel) === identity, 'Package identity changes within a channel.')
      packageIdentities.set(row.channel, identity)
    }
  }
  return report
}

const escape = value => String(value ?? 'Not measured').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c])
const reportUrl = new URL('../reports/sdk-compatibility.json', import.meta.url)
const label = value => value[0].toUpperCase() + value.slice(1)
const statusLabels = { passed: '✓ Passed', incompatible: '⚠ Incompatible', unavailable: '— Unavailable', 'infrastructure-error': '! Infrastructure error' }
const toolFramework = row => row.toolFramework ?? row.toolRuntimeTfm
const columnKey = row => JSON.stringify([row.channel, row.toolVersion, toolFramework(row), row.scenario])
const caseTitle = row => `${label(row.channel)} & ${label(row.scenario)}`
const caseIdentity = row => `${caseTitle(row)}; SDK ${row.sdk}; project ${row.projectTfm}; DocFX ${row.toolVersion ?? 'not measured'}; tool target ${toolFramework(row) ?? 'not measured'}`
const runLink = source => source.repository === 'local' ? null : `https://github.com/${source.repository}/actions/runs/${source.runId}/attempts/${source.runAttempt}`

export function renderReport(report, now = Date.now()) {
  validateReport(report, undefined, now)
  const stale = now - Date.parse(report.generatedAt) > staleAfterDays * 86400000
  const runUrl = runLink(report.source)
  const count = outcome => report.results.filter(r => r.outcome === outcome).length
  const testedDate = new Intl.DateTimeFormat('en-US', { dateStyle: 'medium', timeZone: 'UTC' }).format(new Date(report.generatedAt))
  const summary = [`<strong>${count('passed')} passed</strong>`, `${count('incompatible')} incompatible`]
  if (count('unavailable')) summary.push(`${count('unavailable')} unavailable`)
  if (count('infrastructure-error')) summary.push(`${count('infrastructure-error')} infrastructure error(s)`)
  const columns = [...new Map(report.results.map(row => [columnKey(row), row])).values()].sort((a, b) =>
    ['stable', 'nightly'].indexOf(a.channel) - ['stable', 'nightly'].indexOf(b.channel) ||
    String(a.toolVersion ?? '~').localeCompare(String(b.toolVersion ?? '~'), 'en', { numeric: true }) ||
    String(toolFramework(a) ?? '~').localeCompare(String(toolFramework(b) ?? '~'), 'en', { numeric: true }) ||
    report.scenarios.indexOf(a.scenario) - report.scenarios.indexOf(b.scenario))
  const cells = new Map(report.results.map((row, index) => [`${row.sdk}/${row.projectTfm}/${columnKey(row)}`, index]))
  let html = `<p>Last tested <time datetime="${escape(report.generatedAt)}">${escape(testedDate)}</time></p>`
  html += `<p class="compatibility-counts">${summary.join(' · ')}</p>`
  if (stale) html += `<p class="alert alert-warning"><strong>Stale evidence</strong> — older than ${staleAfterDays} days.</p>`
  if (!report.channels.includes('nightly')) html += '<p class="alert alert-warning"><strong>Current-main package not measured in this report.</strong></p>'
  if (report.stableSelection?.mode === 'explicit-version') html += `<p class="alert alert-warning"><strong>Explicit local version: ${escape(report.stableSelection.requestedVersion)}</strong> — not a measurement of the latest stable release. ${escape(report.stableSelection.reason)}</p>`
  html += '<div class="compatibility-table" role="region" aria-label="SDK compatibility matrix" tabindex="0"><table class="table"><caption class="visually-hidden">SDK and project target by DocFX package and scenario. Each column shows DocFX version and tool target framework. Select a result for details.</caption><thead><tr><th scope="col"><strong>SDK version</strong><span>Project target</span></th>'
  for (const column of columns) html += `<th scope="col"><strong>${escape(caseTitle(column))}</strong><span class="compatibility-tool">${escape(column.toolVersion)} &amp; ${escape(toolFramework(column))}</span></th>`
  html += '</tr></thead><tbody>'
  for (const target of report.matrix) {
    html += `<tr><th scope="row"><code>${escape(target.sdk)}</code><span>${escape(target.projectTfm)}</span></th>`
    for (const column of columns) {
      const index = cells.get(`${target.sdk}/${target.projectTfm}/${columnKey(column)}`)
      const row = report.results[index]
      html += row
        ? `<td><button type="button" class="compatibility-result" data-case-index="${index}" data-outcome="${row.outcome}" aria-label="${escape(caseIdentity(row))}; ${escape(statusLabels[row.outcome])}" aria-pressed="false" aria-expanded="false" aria-controls="compatibility-case">${statusLabels[row.outcome]}</button></td>`
        : '<td><span class="compatibility-unmeasured">Not measured</span></td>'
    }
    html += '</tr>'
  }
  html += `</tbody></table></div>
    <section id="compatibility-case" class="compatibility-case" aria-labelledby="compatibility-case-title" hidden>
      <div class="compatibility-case-heading"><h3 id="compatibility-case-title" tabindex="-1">Case details</h3><button type="button" class="btn btn-sm btn-outline-secondary" data-close>Close details</button></div>
      <p data-case-outcome></p><dl class="compatibility-identity" data-case-identity></dl>
      <h4>Recorded diagnostics</h4><p class="compatibility-diagnostics" data-case-diagnostics></p>
      <p role="status" aria-live="polite" aria-atomic="true" data-log-status></p>
      <button type="button" class="btn btn-sm btn-outline-secondary" data-retry hidden>Retry log</button>
      <div data-excerpt hidden><h4>Diagnostic lines from this log</h4><pre tabindex="0" aria-label="Diagnostic excerpt" data-excerpt-text></pre></div>
      <details data-raw hidden><summary>Raw output</summary><pre tabindex="0" aria-label="Raw case output" data-raw-text></pre></details>
      <div class="compatibility-actions"><button type="button" class="btn btn-sm btn-outline-secondary" data-back>Back to selected result</button><a data-download>Download log</a>${runUrl ? `<a href="${runUrl}" data-run>View CI run</a>` : ''}</div>
    </section>
    <details class="compatibility-provenance"><summary>Report source &amp; downloads</summary>
      <p>${runUrl ? `<a href="${runUrl}">View CI run</a>` : 'Local measurement'}. <a href="${reportUrl.href}" download="sdk-compatibility.json">Download report JSON</a>.</p>
      <p>Measured: ${escape(report.generatedAt)}<br>Source: <code>${escape(report.source.sha)}</code>${report.source.dirty ? ' (with local uncommitted changes)' : ''}${runUrl ? `<br>Run: ${escape(report.source.runId)} · Attempt: ${escape(report.source.runAttempt)}` : ''}.</p>`
  if (report.stableSelection?.mode === 'latest-release') html += `<p>Requested latest stable release: <strong>${escape(report.stableSelection.requestedVersion)}</strong>.</p>`
  return html + '</details>'
}

// Only a validated case's sibling log may be read, never an artifact-supplied URL.
export function caseLogUrl(row, baseUrl = reportUrl) {
  assert(/^logs\/[a-zA-Z0-9.-]+\.log$/.test(row.log), 'Invalid case log path.')
  const url = new URL(row.log, baseUrl)
  assert(['http:', 'https:'].includes(url.protocol) && url.origin === new URL(baseUrl).origin, 'Invalid case log origin.')
  return url
}

export function createCaseSelection(report, onChange, fetchLog = (...args) => fetch(...args), baseUrl = reportUrl) {
  validateReport(report)
  let sequence = 0
  let activeRequest
  let disposed = false
  const cancel = () => { sequence++; activeRequest?.abort() }
  return {
    async select(index) {
      if (disposed) return
      assert(Number.isInteger(index) && report.results[index], 'Unknown compatibility case.')
      cancel()
      const current = sequence
      const row = report.results[index]
      const url = caseLogUrl(row, baseUrl)
      const state = { index, row, logUrl: url.href }
      const request = activeRequest = new AbortController()
      const timeout = setTimeout(() => request.abort(), 30000)
      onChange({ ...state, phase: 'loading' })
      try {
        const response = await fetchLog(url, { signal: request.signal, redirect: 'error', credentials: 'same-origin' })
        if (!response.ok) throw new Error(`HTTP ${response.status}`)
        if (response.redirected || (response.url && response.url !== url.href)) throw new Error('Unexpected log location.')
        if (Number(response.headers?.get('content-length')) > 10 * 1024 * 1024) throw new Error('Log exceeds the display limit.')
        const original = await response.text()
        if (original.length > 10 * 1024 * 1024) throw new Error('Log exceeds the display limit.')
        if (disposed || current !== sequence) return
        const output = original.replace(/\u001b\[[0-?]*[ -/]*[@-~]/g, '')
        const excerpt = row.outcome === 'passed' ? '' : [...new Set(output.split(/\r?\n/).filter(line =>
          /ReferencesNewerCompiler|ReferencedCompilerVersion|\b(?:error|warning)\s+[A-Z]{2,}\d+\b/i.test(line)))].slice(0, 4).join('\n')
        onChange({ ...state, phase: 'ready', output, excerpt })
      } catch (error) {
        if (!disposed && current === sequence) onChange({ ...state, phase: 'error', error: error.name === 'AbortError' ? 'The log request timed out.' : error.message })
      } finally { clearTimeout(timeout) }
    },
    close() { if (!disposed) { cancel(); onChange({ phase: 'closed' }) } },
    dispose() { disposed = true; cancel() },
  }
}

function bindDetails(element, report, fetchLog) {
  const find = selector => element.querySelector(selector)
  const panel = find('#compatibility-case')
  const heading = find('#compatibility-case-title')
  const buttons = [...element.querySelectorAll('[data-case-index]')]
  let selectedButton
  const returnToResult = () => selectedButton?.focus()
  const selection = createCaseSelection(report, state => {
    if (state.phase === 'closed') {
      panel.hidden = true
      panel.setAttribute('aria-busy', 'false')
      selectedButton?.setAttribute('aria-pressed', 'false')
      selectedButton?.setAttribute('aria-expanded', 'false')
      returnToResult()
      return
    }
    const row = state.row
    if (state.phase === 'loading') {
      for (const button of buttons) {
        const selected = Number(button.dataset.caseIndex) === state.index
        button.setAttribute('aria-pressed', String(selected))
        button.setAttribute('aria-expanded', String(selected))
        if (selected) selectedButton = button
      }
      panel.hidden = false
      panel.dataset.caseIndex = String(state.index)
      panel.dataset.outcome = row.outcome
      heading.textContent = `${caseTitle(row)} — ${row.projectTfm}`
      find('[data-case-outcome]').textContent = statusLabels[row.outcome]
      const identity = find('[data-case-identity]')
      identity.replaceChildren()
      for (const [name, value] of [
        ['Requested SDK', row.sdk], ['Selected SDK', row.selectedSdk], ['Project target', row.projectTfm],
        ['DocFX version', row.toolVersion],
        ...(row.toolFramework ? [['Requested tool target', row.toolFramework]] : []),
        ['Tool target framework', row.toolRuntimeTfm], ['OS', row.os],
        ['Tested', row.testedAt], ['Package SHA-256', row.packageSha256],
      ]) {
        const term = element.ownerDocument.createElement('dt')
        const definition = element.ownerDocument.createElement('dd')
        term.textContent = name
        definition.textContent = value ?? 'Not measured'
        identity.append(term, definition)
      }
      find('[data-case-diagnostics]').textContent = row.diagnostics
      find('[data-download]').href = state.logUrl
      find('[data-download]').download = row.log.slice('logs/'.length)
      find('[data-raw]').open = false
      find('[data-raw-text]').textContent = ''
      find('[data-excerpt-text]').textContent = ''
      heading.focus({ preventScroll: true })
      panel.scrollIntoView({ block: 'start' })
    }
    panel.setAttribute('aria-busy', String(state.phase === 'loading'))
    find('[data-log-status]').textContent = state.phase === 'loading' ? 'Loading log…'
      : state.phase === 'error' ? `Log unavailable: ${state.error} The recorded result is unchanged. Retry the log or view the available run evidence.`
        : 'Log loaded.'
    find('[data-retry]').hidden = state.phase !== 'error'
    find('[data-raw]').hidden = state.phase !== 'ready'
    find('[data-excerpt]').hidden = !state.excerpt
    if (state.phase === 'ready') {
      find('[data-raw-text]').textContent = state.output
      find('[data-excerpt-text]').textContent = state.excerpt
    }
  }, fetchLog)
  const onClick = event => {
    const button = event.target.closest('button')
    if (!button || !element.contains(button)) return
    if (button.hasAttribute('data-case-index')) void selection.select(Number(button.dataset.caseIndex))
    else if (button.hasAttribute('data-close')) selection.close()
    else if (button.hasAttribute('data-back')) returnToResult()
    else if (button.hasAttribute('data-retry')) void selection.select(Number(selectedButton.dataset.caseIndex))
  }
  element.addEventListener('click', onClick)
  return () => { selection.dispose(); element.removeEventListener('click', onClick) }
}

const loads = new WeakMap()
export async function loadReport(element, fetchReport = (...args) => fetch(...args)) {
  loads.get(element)?.dispose()
  const request = new AbortController()
  let unbind = () => {}
  const current = { dispose() { request.abort(); unbind(); if (loads.get(element) === current) loads.delete(element) } }
  loads.set(element, current)
  element.textContent = 'Loading compatibility evidence…'
  try {
    const response = await fetchReport(reportUrl, { signal: request.signal, redirect: 'error', credentials: 'same-origin' })
    if (!response.ok) throw new Error('No compatibility report is available in this site build.')
    const data = await response.json()
    if (loads.get(element) !== current) return current.dispose
    if (data.state === 'unavailable') throw new Error(text(data.reason) ? data.reason : 'Compatibility evidence is unavailable.')
    element.innerHTML = renderReport(data)
    unbind = bindDetails(element, data, fetchReport)
  } catch (error) {
    if (loads.get(element) === current) element.textContent = `Compatibility evidence unavailable. ${error.message}`
  }
  return current.dispose
}

if (typeof document !== 'undefined') {
  const element = document.getElementById('sdk-compatibility-report')
  if (element) await loadReport(element)
}
