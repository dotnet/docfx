function exec([string] $cmd) {
    Write-Host $cmd -ForegroundColor Green
    & ([scriptblock]::Create($cmd))
    if ($lastexitcode -ne 0) {
        throw ("Error: " + $cmd)
    }
}

exec "dotnet test test\docfx.Test -c Release"
exec "dotnet pack src\docfx -c Release -o $PSScriptRoot\drop"
