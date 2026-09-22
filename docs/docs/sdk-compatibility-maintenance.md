---
uid: sdk-compatibility-maintenance
---

# Maintaining SDK compatibility checks

This guide covers the measurement and publication pipeline behind the [SDK compatibility report](xref:sdk-compatibility).

## Packages and SDK selection

The nightly workflow builds the checked-out main commit, packs it, and tests **that exact package artifact**, verifying its version and package hash rather than installing whichever prerelease a feed returns. A separate released-package group discovers the latest non-prerelease version from the official GitHub release metadata, then installs and verifies that exact version using the configured package feed. Discovery or installation failure is an **infrastructure-error**; there is no fallback to an older release. GitHub release metadata is used only for version discovery, not package downloads.

Every case records the selected project SDK, project target framework, package version and hash, tool runtime TFM, OS, scenario, and timestamp. The report records the workflow's source commit and stable-version selection. Runtime TFM describes the packaged tool, not the project's framework or the exact installed runtime patch.

Both DocFX package groups use the same Linux matrix: the latest SDK from the explicitly configured 8.0, 9.0, 10.0, and 11.0 channels, resolved from Microsoft's official release metadata. Latest stable and current preview refer to **DocFX packages**, not SDK channels. The five SDK/project-TFM pairs are 8.0/`net8.0`, 9.0/`net9.0`, 10.0/`net10.0`, 11.0/`net10.0`, and 11.0/`net11.0`. With two packages and two scenarios, this configures **20 cases per run**. The same exact 11.0 SDK tests both `net10.0` for older-project regression coverage and `net11.0` for new-target coverage; their results and logs remain separate. This is configured coverage, not evidence that unmeasured combinations passed.

Each SDK is installed once, even when it tests multiple project TFMs. Each case pins `global.json` with roll-forward disabled and verifies `dotnet --version`. DocFX still runs from the package's `net10.0` assets regardless of project TFM; other tool TFMs and operating systems are not covered by this matrix.

DocFX bundles a tested Roslyn dependency set. Selecting a newer SDK does not replace those assemblies. SDK source generators can require a newer compiler even when a project targets an older framework. This report measures that interaction; it does not change compiler loading or prescribe a workaround.

## Scenarios and outcomes

- **basic** compiles a C# class and checks that its public type and method appear in API metadata.
- **razor** compiles a Razor component with `@inherits` and a code-behind override. Metadata must contain both the public API and its generated inheritance/override relationship. An incomplete API is not a pass, even if DocFX exits successfully.

Outcomes distinguish compatibility observations from cases that could not be measured:

- **passed**: fixture build, metadata extraction, and API assertions passed.
- **incompatible**: DocFX metadata extraction, generator diagnostics, or expected API checks failed after the fixture compiled successfully. Read the log before attributing the failure to a particular dependency.
- **unavailable**: the requested SDK could not be selected; no compatibility measurement was made.
- **infrastructure-error**: installation, restore, fixture build, or harness setup failed; no pass is inferred.

Normal and validation-only nightlies use **reporting mode**: a complete report containing incompatible results is a valid observation, not a failing compatibility job. Findings remain labeled incompatible in the report, with workflow warnings and a step summary. Unavailable cases, infrastructure errors, invalid provenance, malformed or incomplete reports, and script errors still fail. The existing source build, unit tests, and seed-site checks remain required. PR packaged-tool smoke uses **strict mode**, where any non-passing case fails.

## Evidence and website publication

Evidence older than seven days is labeled **stale**, including when the page is revisited without a new website deployment. Missing, expired, inaccessible, or invalid reports never produce passing rows.

The report's source, run attempt, timestamp, and original JSON are available under **Report source & downloads**; each selected case also shows its environment and package hash. Selecting a case reads recorded evidence, not a new measurement. Logs load on demand, raw output is expandable, and **Download log** preserves the original bytes. A missing or unreadable log shows an error with a retry action without changing the recorded outcome.

Only the case logs named in the validated report are copied into the site, not unrelated artifact files or installation/environment logs. Log paths, duplicate ZIP entries, and extraction sizes are checked; artifact content is never executed.

Open the linked workflow run and download **sdk-compatibility-v1** for the full report and diagnostic logs. Artifacts are retained for **14 days**, not permanently archived. A complete nightly report can contain incompatible observations or infrastructure errors; publication does not select only successful workflow runs. Website builds accept reports only from this repository's completed, main-branch scheduled or manually dispatched nightly workflow, and validate the report's commit, run ID, attempt, schema, and complete matrix.

Main-branch CI publication and completed nightlies use the same whole-site publishing workflow. A nightly refresh requires successful CI for the current main revision. Normal docs publishing reuses retained evidence; it does not rerun the SDK matrix. No result commits are needed.

## Validating a workflow change

Manual runs of `nightly.yml` default to **validation only**. Select the branch to test and leave `validation_only` enabled. The workflow builds that checkout, runs the existing tests on all three tool target frameworks, packs it, and downloads the same versioned package for seed-site checks and the complete SDK compatibility matrix. Both this exact branch package and the latest stable release are tested against the same SDK matrix. This mode only restricts publication: it cannot publish NuGet packages or deploy Pages. It uploads the exact package and **sdk-compatibility-validation-v1** report/log artifact for inspection, retained for 14 days. Incompatible observations do not block the run; infrastructure failures still fail and retain available evidence.

Scheduled main runs keep the normal released-package comparison and publication behavior. A manual production run requires explicitly disabling `validation_only` and selecting upstream main. The website only refreshes for nightlies carrying the separate production report artifact; validation artifacts cannot authorize publication, even when validation runs on main.

Validation uses the repository and runner's existing package-source configuration; it does not silently change feeds. Confirm that those sources are authorized before dispatching a remote run. A locally restricted mirror's missing dependencies do not establish whether the configured GitHub-hosted build can restore them. Manual dispatch also requires write access and an existing workflow on the repository's default branch; adding a workflow only to a branch of an otherwise unconfigured fork is insufficient.

## Running locally

From the repository root, resolve the matrix and install the SDKs you intend to test:

```powershell
node test/compatibility/report.mjs resolve drop/compatibility/matrix.json
./test/compatibility/Measure-Compatibility.ps1 -MatrixPath drop/compatibility/matrix.json -OutputDirectory drop/compatibility/report -NightlyPackage path/to/exact/docfx.nupkg
node test/compatibility/report.mjs prepare drop/compatibility/report/compatibility-report.json docs/obj/sdk-compatibility.json
docfx docs/docfx.json
```

Omit `-NightlyPackage` for released-package-only local measurements, or use `-SkipStable` for a bounded packaged-tool smoke. A report without a nightly package does **not** validate current main. For a deliberately older local comparison, supply both `-StableVersion` and `-StableVersionReason`; this is labeled an explicit local version, never the latest stable release, and is not allowed in GitHub Actions. `-NuGetConfig` applies an explicit feed configuration to tool installation and fixture restores; reuse your organization's approved feeds. `-WorkDirectory` retains isolated package/fixture files for inspection and must name a new directory. The harness validates the completed report and explicitly returns its check result, rather than inheriting the last fixture's native exit code. By default, incompatible observations produce warnings without failing; unavailable cases, infrastructure errors, and invalid or incomplete reports always fail **after** writing available evidence. `-FailOnIncompatible` selects strict mode for PR smoke or local gates.

To recheck a saved report without rerunning measurements, use `node test/compatibility/report.mjs check path/to/compatibility-report.json`. Add `--strict` to require every row to pass. Neither mode rewrites observations or package provenance.

Run fast reporting and publication-contract tests with `node --test test/compatibility/report.test.mjs`. Generated evidence belongs in ignored `drop` and `docs/obj` directories, not in commits.
