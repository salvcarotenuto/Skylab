param(
    [int]$Port = 5187,
    [string]$OpenPath = "/",
    [switch]$NoBuild,
    [switch]$NoBrowser
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "src\SkyLab.Web\SkyLab.Web.csproj"
$projectFullPath = [System.IO.Path]::GetFullPath($project)
$outLog = Join-Path $root "skylab-run.out.log"
$errLog = Join-Path $root "skylab-run.err.log"
$pidFile = Join-Path $root "skylab-run.pid"
$baseUrl = "http://localhost:$Port"

if (-not $OpenPath.StartsWith("/")) {
    $OpenPath = "/$OpenPath"
}
$targetUrl = "$baseUrl$OpenPath"

function Stop-SkylabProcess {
    $processes = Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object {
        ($_.Name -eq "dotnet.exe" -and $_.CommandLine -and $_.CommandLine.Contains($projectFullPath)) -or
        ($_.Name -eq "SkyLab.Web.exe" -and $_.ExecutablePath -and $_.ExecutablePath.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase))
    }

    foreach ($item in $processes) {
        Stop-Process -Id $item.ProcessId -Force -ErrorAction SilentlyContinue
    }

    if (Test-Path -LiteralPath $pidFile) {
        $storedPid = 0
        if ([int]::TryParse((Get-Content -LiteralPath $pidFile -Raw).Trim(), [ref]$storedPid)) {
            Stop-Process -Id $storedPid -Force -ErrorAction SilentlyContinue
        }
        Remove-Item -LiteralPath $pidFile -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "Riavvio SkyLab su $baseUrl"
Stop-SkylabProcess

if (-not $NoBuild) {
    Write-Host "Compilazione dell'applicazione..."
    & dotnet build $project --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Compilazione non riuscita. Il browser non verra aperto."
    }
}

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:DOTNET_ENVIRONMENT = "Development"

$runArguments = @(
    "run", "--no-build", "--no-launch-profile",
    "--project", $project,
    "--urls", $baseUrl
)

$process = Start-Process -FilePath "dotnet" `
    -ArgumentList $runArguments `
    -WorkingDirectory $root `
    -RedirectStandardOutput $outLog `
    -RedirectStandardError $errLog `
    -WindowStyle Hidden `
    -PassThru

Set-Content -LiteralPath $pidFile -Value $process.Id

$ready = $false
foreach ($attempt in 1..120) {
    if ($process.HasExited) {
        break
    }

    try {
        $response = Invoke-WebRequest -Uri $baseUrl -UseBasicParsing -TimeoutSec 2
        if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
            $ready = $true
            break
        }
    }
    catch {
        Start-Sleep -Milliseconds 500
    }
}

if (-not $ready) {
    Write-Host "SkyLab non ha risposto entro il tempo previsto."
    Write-Host "Controllare i log:"
    Write-Host "  $outLog"
    Write-Host "  $errLog"
    exit 1
}

Write-Host "SkyLab pronto: $targetUrl"
if (-not $NoBrowser) {
    Start-Process $targetUrl
}

exit 0
