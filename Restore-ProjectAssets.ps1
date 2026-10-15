$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$manifest = Get-Content -LiteralPath (Join-Path $root '.project-storage/manifest.json') -Raw | ConvertFrom-Json
foreach ($item in $manifest) {
    $target = [IO.Path]::GetFullPath((Join-Path $root $item.path))
    if (-not $target.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid target path.' }
    if (Test-Path -LiteralPath $target) {
        if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ine $item.sha256) { throw ('Existing file differs: ' + $item.path) }
        Write-Output ('Verified: ' + $item.path)
        continue
    }
    foreach ($part in $item.parts) {
        $partPath = [IO.Path]::GetFullPath((Join-Path $root $part.path))
        if (-not $partPath.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid part path.' }
        if ((Get-FileHash -LiteralPath $partPath -Algorithm SHA256).Hash -ine $part.sha256) { throw ('Part checksum failed: ' + $part.path) }
    }
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
    $output = [IO.File]::Open($target, [IO.FileMode]::CreateNew)
    try {
        foreach ($part in $item.parts) {
            $inputStream = [IO.File]::OpenRead((Join-Path $root $part.path))
            try { $inputStream.CopyTo($output) } finally { $inputStream.Dispose() }
        }
    } finally { $output.Dispose() }
    if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ine $item.sha256) { throw ('Restored checksum failed: ' + $item.path) }
    Write-Output ('Restored: ' + $item.path)
}
Write-Output 'All project assets are ready.'
