# Read only the report; never extract or execute artifact content.
param([Parameter(Mandatory)][string] $ArchivePath)
$ErrorActionPreference = 'Stop'
$archive = [IO.Compression.ZipFile]::OpenRead($ArchivePath)
try {
    $entries = @($archive.Entries | Where-Object FullName -eq 'compatibility-report.json')
    if ($entries.Count -ne 1 -or $entries[0].Length -gt 1MB) { throw 'Artifact must contain exactly one report, no larger than 1 MiB.' }
    $reader = [IO.StreamReader]::new($entries[0].Open())
    try { [Console]::Write($reader.ReadToEnd()) } finally { $reader.Dispose() }
} finally { $archive.Dispose() }
