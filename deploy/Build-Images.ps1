[CmdletBinding()]
param(
    [string]$Registry = "ecohover",
    [switch]$Push,
    [switch]$AllowDirty,
    [switch]$PrintOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$version = [System.IO.File]::ReadAllText((Join-Path $repositoryRoot "VERSION")).Trim()
if ($version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+([+-][0-9A-Za-z.-]+)?$') {
    throw "VERSION is not a supported semantic version: $version"
}

Push-Location $repositoryRoot
try {
    $shortSha = (& git rev-parse --short=7 HEAD).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to resolve the current Git commit."
    }

    $apiVersionTag = "${Registry}/khaikang-api:${version}"
    $apiShaTag = "${Registry}/khaikang-api:sha-${shortSha}"
    $webVersionTag = "${Registry}/khaikang-web:${version}"
    $webShaTag = "${Registry}/khaikang-web:sha-${shortSha}"
    $imageTags = @($apiVersionTag, $apiShaTag, $webVersionTag, $webShaTag)

    Write-Host "KhaiKang version: $version"
    $imageTags | ForEach-Object { Write-Host "  $_" }
    if ($PrintOnly) {
        return
    }

    if (-not $AllowDirty -and (& git status --porcelain)) {
        throw "The working tree is not clean. Commit the release content or pass -AllowDirty for a local-only build."
    }

    & docker build --build-arg "APP_VERSION=$version" --tag $apiVersionTag --tag $apiShaTag --file deploy/docker/api.Dockerfile .
    if ($LASTEXITCODE -ne 0) {
        throw "API image build failed."
    }

    & docker build --build-arg "APP_VERSION=$version" --tag $webVersionTag --tag $webShaTag --file deploy/docker/web.Dockerfile .
    if ($LASTEXITCODE -ne 0) {
        throw "Web image build failed."
    }

    if ($Push) {
        foreach ($imageTag in $imageTags) {
            & docker push $imageTag
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to push $imageTag."
            }
        }
    }
}
finally {
    Pop-Location
}
