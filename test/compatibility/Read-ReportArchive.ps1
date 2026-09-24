# Read the report first. The caller validates its schema and provenance before exporting named evidence.
param(
    [Parameter(Mandatory)][string] $ArchivePath,
    [string] $EvidenceDirectory
)
$ErrorActionPreference = 'Stop'
function Copy-Entry($Entry, $Output) {
    $stream = $Entry.Open()
    try {
        $buffer = [byte[]]::new(81920)
        $size = 0L
        while (($count = $stream.Read($buffer, 0, $buffer.Length)) -gt 0) {
            $size += $count
            if ($size -gt $Entry.Length) { throw 'ZIP entry exceeds its declared size.' }
            $Output.Write($buffer, 0, $count)
        }
        if ($size -ne $Entry.Length) { throw 'ZIP entry is incomplete.' }
    } finally { $stream.Dispose() }
}
$archive = [IO.Compression.ZipFile]::OpenRead($ArchivePath)
try {
    $entries = @($archive.Entries | Where-Object FullName -CEQ 'compatibility-report.json')
    if ($entries.Count -ne 1 -or $entries[0].Length -gt 1MB) { throw 'Artifact must contain exactly one report, no larger than 1 MiB.' }
    $reportEntry = $entries[0]
    $buffer = [IO.MemoryStream]::new()
    try {
        Copy-Entry $reportEntry $buffer
        $buffer.Position = 0
        $reader = [IO.StreamReader]::new($buffer)
        try { $json = $reader.ReadToEnd() } finally { $reader.Dispose() }
    } finally { $buffer.Dispose() }
    if ($EvidenceDirectory) {
        if (Test-Path $EvidenceDirectory) { throw 'Evidence directory must be new.' }
        $report = $json | ConvertFrom-Json
        # At most 12 SDK/project pairs, 8 package/tool targets, and 2 scenarios.
        if (!$report.results -or $report.results.Count -gt 192) { throw 'Invalid case log manifest.' }
        $names = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        $destinations = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        $logs = @()
        $size = 0L
        foreach ($row in $report.results) {
            if ($row.log -cnotmatch '^logs/[a-zA-Z0-9.-]+\.log$') { throw 'Invalid case log path.' }
            if (!$names.Add($row.log)) { continue }
            if (!$destinations.Add($row.log)) { throw 'Case log names collide on a case-insensitive filesystem.' }
            $matches = @($archive.Entries | Where-Object FullName -CEQ $row.log)
            if ($matches.Count -gt 1) { throw 'Duplicate case log ZIP entry.' }
            if (!$matches.Count) { continue } # Missing logs remain missing evidence, not a different result.
            if ($matches[0].Length -gt 10MB) { throw 'Case log exceeds 10 MiB.' }
            $size += $matches[0].Length
            if ($size -gt 50MB) { throw 'Case logs exceed 50 MiB.' }
            $logs += $matches[0]
        }
        [IO.Directory]::CreateDirectory((Join-Path $EvidenceDirectory 'logs')) > $null
        foreach ($entry in @($reportEntry) + $logs) {
            $target = if ($entry -eq $reportEntry) { Join-Path $EvidenceDirectory 'compatibility-report.json' } else { Join-Path $EvidenceDirectory 'logs' ($entry.FullName.Substring(5)) }
            $outputStream = [IO.File]::Open($target, [IO.FileMode]::CreateNew)
            try { Copy-Entry $entry $outputStream } finally { $outputStream.Dispose() }
        }
    }
    [Console]::Write($json)
} finally { $archive.Dispose() }
