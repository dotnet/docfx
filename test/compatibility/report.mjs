// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { readFile, writeFile, mkdir, mkdtemp, rm } from 'node:fs/promises'
import { dirname, join } from 'node:path'
import { tmpdir } from 'node:os'
import { fileURLToPath, pathToFileURL } from 'node:url'
import { execFileSync } from 'node:child_process'
import { validateReport } from '../../docs/template/public/sdk-compatibility.mjs'

const repository = 'dotnet/docfx'
const artifactName = 'sdk-compatibility-v1'
const here = dirname(fileURLToPath(import.meta.url))
const json = async path => JSON.parse((await readFile(path, 'utf8')).replace(/^\uFEFF/, ''))
const save = async (path, data) => { await mkdir(dirname(path), { recursive: true }); await writeFile(path, JSON.stringify(data, null, 2) + '\n') }

export function resolveMatrix(config, index) {
  return config.channels.map(target => {
    const release = index['releases-index'].find(r => r['channel-version'] === target.channel)
    if (!release || !/^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$/.test(release['latest-sdk'])) throw new Error(`No exact SDK in official metadata for ${target.channel}.`)
    return { ...target, sdk: release['latest-sdk'], supportPhase: release['support-phase'] }
  })
}

export function resolveStableRelease(release) {
  if (release.draft !== false || release.prerelease !== false || !/^v?\d+\.\d+\.\d+$/.test(release.tag_name)) throw new Error('Latest official release is not an exact stable DocFX version.')
  return { mode: 'latest-release', requestedVersion: release.tag_name.replace(/^v/, ''), reason: 'Latest stable GitHub release; installation must use this exact version without fallback.' }
}

export function trustedRun(run, workflowId) {
  return run.workflow_id === workflowId && run.path === '.github/workflows/nightly.yml' && run.head_repository?.full_name === repository &&
    run.head_branch === 'main' && ['schedule', 'workflow_dispatch'].includes(run.event) && run.status === 'completed' &&
    /^[a-f0-9]{40}$/.test(run.head_sha) && Number.isSafeInteger(run.id) && Number.isSafeInteger(run.run_attempt)
}

export async function latestReport(api, readArchive, expectedMatrix) {
  const workflow = await api(`/repos/${repository}/actions/workflows/nightly.yml`)
  // Bounded pagination covers far more than the 14-day artifact retention window.
  for (let page = 1; page <= 5; page++) {
    const { workflow_runs: runs } = await api(`/repos/${repository}/actions/workflows/${workflow.id}/runs?branch=main&status=completed&per_page=100&page=${page}`)
    for (const run of runs) {
      if (!trustedRun(run, workflow.id)) continue
      let artifact
      for (let artifactPage = 1; artifactPage <= 5; artifactPage++) {
        const { artifacts } = await api(`/repos/${repository}/actions/runs/${run.id}/artifacts?per_page=100&page=${artifactPage}`)
        const matches = artifacts.filter(a => a.name === artifactName && !a.expired)
        if (matches.length > 1) throw new Error('Duplicate report artifacts in a trusted run.')
        artifact = matches[0]
        if (artifact || artifacts.length < 100) break
      }
      if (!artifact) continue
      if (artifact.workflow_run?.id !== run.id || artifact.workflow_run?.head_sha !== run.head_sha || artifact.workflow_run?.head_branch !== 'main') throw new Error('Artifact provenance does not match its run.')
      if (!Number.isSafeInteger(artifact.id) || artifact.size_in_bytes > 50 * 1024 * 1024) throw new Error('Invalid report artifact size or identity.')
      let report
      try { report = await readArchive(artifact.id) } catch (error) {
        if (error.status === 404 || error.status === 410) continue // Expired between listing and download.
        throw error
      }
      validateReport(report, { repository, sha: run.head_sha, runId: run.id, runAttempt: run.run_attempt })
      if (expectedMatrix) {
        const key = t => `${t.channel}/${t.projectTfm}`
        if (JSON.stringify(report.matrix.map(key).sort()) !== JSON.stringify(expectedMatrix.map(key).sort()) || report.matrix.some(t => !t.sdk.startsWith(`${t.channel}.`))) throw new Error('Report differs from the reviewed SDK channel matrix.')
      }
      return report
    }
    if (runs.length < 100) break
  }
  return { state: 'unavailable', reason: 'No complete retained compatibility report was found in trusted main nightly runs.' }
}

export async function productionReportReady(api, runId) {
  if (!/^\d+$/.test(String(runId))) throw new Error('Invalid workflow run identity.')
  const workflow = await api(`/repos/${repository}/actions/workflows/nightly.yml`)
  const run = await api(`/repos/${repository}/actions/runs/${runId}`)
  if (!trustedRun(run, workflow.id) || String(run.id) !== String(runId)) return false
  for (let page = 1; page <= 5; page++) {
    const { artifacts } = await api(`/repos/${repository}/actions/runs/${runId}/artifacts?per_page=100&page=${page}`)
    const matches = artifacts.filter(a => a.name === artifactName && !a.expired)
    if (matches.length > 1) throw new Error('Duplicate report artifacts in a trusted run.')
    if (matches.length) {
      const artifact = matches[0]
      if (artifact.workflow_run?.id !== run.id || artifact.workflow_run?.head_sha !== run.head_sha || artifact.workflow_run?.head_branch !== 'main') throw new Error('Artifact provenance does not match its run.')
      return true
    }
    if (artifacts.length < 100) break
  }
  return false
}

export async function findSiteRun(api, sha) {
  if (!/^[a-f0-9]{40}$/.test(sha)) throw new Error('Invalid source commit.')
  const workflow = await api(`/repos/${repository}/actions/workflows/ci.yml`)
  const { workflow_runs: runs } = await api(`/repos/${repository}/actions/workflows/${workflow.id}/runs?branch=main&event=push&head_sha=${sha}&status=success&per_page=100`)
  const run = runs.find(r => r.workflow_id === workflow.id && r.path === '.github/workflows/ci.yml' && r.head_repository?.full_name === repository && r.head_sha === sha && r.head_branch === 'main' && r.event === 'push' && r.status === 'completed' && r.conclusion === 'success')
  let artifact
  if (run) {
    for (let page = 1; page <= 5; page++) {
      const { artifacts } = await api(`/repos/${repository}/actions/runs/${run.id}/artifacts?per_page=100&page=${page}`)
      artifact = artifacts.find(a => a.name === 'docs-site' && !a.expired && a.workflow_run?.head_sha === sha && a.workflow_run?.id === run.id)
      if (artifact || artifacts.length < 100) break
    }
  }
  return { ready: Boolean(run), runId: run?.id ?? '', artifactId: artifact?.id ?? '' }
}

async function request(path, binary = false) {
  const response = await fetch(`https://api.github.com${path}`, {
    headers: { Accept: 'application/vnd.github+json', 'X-GitHub-Api-Version': '2022-11-28', ...(process.env.GH_TOKEN ? { Authorization: `Bearer ${process.env.GH_TOKEN}` } : {}) },
    signal: AbortSignal.timeout(60000),
  }).catch(error => { error.transport = true; throw error })
  if (!response.ok) {
    const error = new Error(`GitHub evidence request returned HTTP ${response.status}.`)
    error.status = response.status
    throw error
  }
  if (!binary) return response.json()
  if (Number(response.headers.get('content-length')) > 50 * 1024 * 1024) throw new Error('Report artifact exceeds size limit.')
  const chunks = []
  let size = 0
  for await (const chunk of response.body) {
    size += chunk.length
    if (size > 50 * 1024 * 1024) throw new Error('Report artifact exceeds size limit.')
    chunks.push(chunk)
  }
  return Buffer.concat(chunks)
}

async function archiveReport(id) {
  const temp = await mkdtemp(join(tmpdir(), 'docfx-report-'))
  try {
    const path = join(temp, 'report.zip')
    await writeFile(path, await request(`/repos/${repository}/actions/artifacts/${id}/zip`, true))
    return JSON.parse(execFileSync('pwsh', ['-NoProfile', '-File', join(here, 'Read-ReportArchive.ps1'), '-ArchivePath', path], { encoding: 'utf8', maxBuffer: 1024 * 1024 }).replace(/^\uFEFF/, ''))
  } finally { await rm(temp, { recursive: true, force: true }) }
}

export async function main([command, path, output]) {
  switch (command) {
    case 'stable-version': {
      console.log(JSON.stringify(resolveStableRelease(await request(`/repos/${repository}/releases/latest`))))
      break
    }
    case 'resolve': {
      const config = await json(join(here, 'sdk-matrix.json'))
      const response = await fetch(config.releaseIndex, { signal: AbortSignal.timeout(60000) })
      if (!response.ok) throw new Error(`SDK metadata returned HTTP ${response.status}.`)
      const matrix = resolveMatrix(config, await response.json())
      await save(path, matrix)
      if (process.env.GITHUB_OUTPUT) await writeFile(process.env.GITHUB_OUTPUT, `sdks<<SDK_LIST\n${[...new Set(matrix.map(t => t.sdk))].join('\n')}\nSDK_LIST\n`, { flag: 'a' })
      break
    }
    case 'prepare': {
      await save(output, validateReport(await json(path)))
      break
    }
    case 'fetch': {
      let report
      try { report = await latestReport(request, archiveReport, (await json(join(here, 'sdk-matrix.json'))).channels) } catch (error) {
        // Transport/permission failures are absence of evidence, not compatibility failures.
        // Malformed data or provenance mismatches fail the build instead of publishing it.
        if (error.status || error.name === 'TimeoutError' || error.transport) {
          report = { state: 'unavailable', reason: `Compatibility evidence could not be retrieved: ${error.message}` }
        } else { throw error }
      }
      await save(path, report)
      break
    }
    case 'check': {
      const report = validateReport(await json(path))
      if (report.results.some(r => r.outcome !== 'passed')) process.exitCode = 1
      break
    }
    case 'production-ready': {
      const ready = await productionReportReady(request, path)
      if (process.env.GITHUB_OUTPUT) await writeFile(process.env.GITHUB_OUTPUT, `ready=${ready}\n`, { flag: 'a' })
      break
    }
    case 'site-run': {
      const site = await findSiteRun(request, path)
      if (process.env.GITHUB_OUTPUT) await writeFile(process.env.GITHUB_OUTPUT, `ready=${site.ready}\nrun-id=${site.runId}\nartifact-id=${site.artifactId}\n`, { flag: 'a' })
      break
    }
    default: throw new Error('Usage: report.mjs stable-version | resolve <matrix.json> | fetch <output.json> | prepare <report.json> <output.json> | check <report.json> | production-ready <run-id> | site-run <sha>')
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  await main(process.argv.slice(2))
}
