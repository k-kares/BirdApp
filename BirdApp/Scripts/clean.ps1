$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot

Set-Location $ProjectRoot

Write-Host "=== BirdApp clean ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Ovo će obrisati Docker containere I njihove volumene." -ForegroundColor Red
Write-Host "Audio folder u projektu NEĆE biti obrisan." -ForegroundColor Green
Write-Host ""

$confirmation = Read-Host "Upiši YES za nastavak"

if ($confirmation -ne "YES") {
    Write-Host "Čišćenje otkazano."
    exit
}

docker compose down -v

if ($LASTEXITCODE -ne 0) {
    throw "docker compose down -v nije uspio."
}

Write-Host ""
Write-Host "Sustav je očišćen." -ForegroundColor Green
Write-Host "MongoDB, MinIO i Kafka podaci su uklonjeni." -ForegroundColor Yellow