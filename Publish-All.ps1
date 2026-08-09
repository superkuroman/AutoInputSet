[CmdletBinding()]
param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",

    [ValidateSet("win-x64", "win-arm64")]
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$repositoryRoot = $PSScriptRoot
$publishRoot = Join-Path $repositoryRoot "ROM\Publish"
$frameworkDependentOutput = Join-Path $publishRoot "FrameworkDependent"
$selfContainedOutput = Join-Path $publishRoot "SelfContained"
$projects = @(
    "GameAutomation.Capture\GameAutomation.Capture.csproj",
    "GameAutomation.Editor\GameAutomation.Editor.csproj",
    "GameAutomation.Runner\GameAutomation.Runner.csproj"
)

function Reset-PublishDirectory {
    param([Parameter(Mandatory)][string]$Path)

    $expectedRoot = [System.IO.Path]::GetFullPath($publishRoot).TrimEnd('\') + '\'
    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    if (-not $resolvedPath.StartsWith($expectedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clear an unexpected path: $resolvedPath"
    }

    if (Test-Path -LiteralPath $resolvedPath) {
        Remove-Item -LiteralPath $resolvedPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $resolvedPath | Out-Null
}

function Publish-Projects {
    param(
        [Parameter(Mandatory)][string]$Output,
        [Parameter(Mandatory)][bool]$SelfContained
    )

    Reset-PublishDirectory -Path $Output

    foreach ($project in $projects) {
        $projectPath = Join-Path $repositoryRoot $project
        Write-Host "Publishing $project..." -ForegroundColor Cyan

        $arguments = @(
            "publish",
            $projectPath,
            "--configuration", $Configuration,
            "--runtime", $Runtime,
            "--output", $Output,
            "--nologo",
            "-p:DebugType=None",
            "-p:DebugSymbols=false"
        )

        if ($SelfContained) {
            $arguments += "--self-contained"
        }
        else {
            $arguments += "--no-self-contained"
        }

        & dotnet @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Publish failed: $project"
        }
    }

    foreach ($contentDirectory in @("Templates", "Scripts")) {
        $source = Join-Path $repositoryRoot $contentDirectory
        $destination = Join-Path $Output $contentDirectory
        New-Item -ItemType Directory -Path $destination -Force | Out-Null

        if (Test-Path -LiteralPath $source) {
            Get-ChildItem -LiteralPath $source -Force |
                Where-Object Name -ne ".gitkeep" |
                Copy-Item -Destination $destination -Recurse -Force
        }
    }

    New-Item -ItemType Directory -Path (Join-Path $Output "Logs") -Force | Out-Null
}

Publish-Projects -Output $frameworkDependentOutput -SelfContained $false
Publish-Projects -Output $selfContainedOutput -SelfContained $true

Write-Host ""
Write-Host "Publish complete:" -ForegroundColor Green
Write-Host "  Framework-dependent: $frameworkDependentOutput"
Write-Host "  Self-contained:       $selfContainedOutput"
