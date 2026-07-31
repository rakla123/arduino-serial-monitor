param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ProjectRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$SolutionPath = Join-Path $ProjectRoot 'ArduinoSerialMonitor.sln'
$DistDirectory = Join-Path $ProjectRoot 'dist'
$WorkDirectory = Join-Path $ProjectRoot 'work\release'
$PackageName = "Arduino-Serial-Monitor-$Version"
$StageDirectory = Join-Path $WorkDirectory $PackageName
$ZipPath = Join-Path $DistDirectory "$PackageName.zip"
$ChecksumPath = "$ZipPath.sha256"

function Assert-ProjectChildPath([string]$Path) {
    $fullPath = [IO.Path]::GetFullPath($Path)
    $rootPrefix = $ProjectRoot.TrimEnd('\') + '\'
    if (-not $fullPath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing operation outside the project directory: $fullPath"
    }
    return $fullPath
}

function Find-MSBuild {
    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $vswhere) {
        $installation = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
        if ($installation) {
            $candidate = Join-Path $installation 'MSBuild\Current\Bin\MSBuild.exe'
            if (Test-Path -LiteralPath $candidate) {
                return $candidate
            }
        }
    }

    throw 'MSBuild was not found. Install Visual Studio with the .NET desktop development workload.'
}

$DistDirectory = Assert-ProjectChildPath $DistDirectory
$WorkDirectory = Assert-ProjectChildPath $WorkDirectory
$StageDirectory = Assert-ProjectChildPath $StageDirectory

foreach ($path in @($WorkDirectory, $DistDirectory)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}

New-Item -ItemType Directory -Path $StageDirectory, $DistDirectory -Force | Out-Null

$msbuild = Find-MSBuild
& $msbuild $SolutionPath /restore /m /p:Configuration=Release /p:Platform='Any CPU' /verbosity:minimal
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild failed with exit code $LASTEXITCODE."
}

$buildDirectory = Join-Path $ProjectRoot 'src\ArduinoSerialMonitor\bin\Release'
$executable = Join-Path $buildDirectory 'ArduinoSerialMonitor.exe'
$configuration = "$executable.config"
if (-not (Test-Path -LiteralPath $executable)) {
    throw "Expected executable was not created: $executable"
}

Copy-Item -LiteralPath $executable -Destination $StageDirectory
if (Test-Path -LiteralPath $configuration) {
    Copy-Item -LiteralPath $configuration -Destination $StageDirectory
}

Copy-Item -LiteralPath (Join-Path $ProjectRoot 'README.md'),
                           (Join-Path $ProjectRoot 'KNOWN-LIMITATIONS.md'),
                           (Join-Path $ProjectRoot 'LICENSE.md'),
                           (Join-Path $ProjectRoot 'THIRD-PARTY-NOTICES.md'),
                           (Join-Path $ProjectRoot 'CHANGELOG.md') -Destination $StageDirectory
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'docs') -Destination $StageDirectory -Recurse
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'firmware') -Destination $StageDirectory -Recurse

Compress-Archive -LiteralPath $StageDirectory -DestinationPath $ZipPath -CompressionLevel Optimal
$hash = (Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText($ChecksumPath, "$hash  $PackageName.zip`n", [Text.UTF8Encoding]::new($false))

Write-Host 'Created release package:'
Write-Host $ZipPath
Write-Host $ChecksumPath
Write-Host "SHA-256: $hash"
