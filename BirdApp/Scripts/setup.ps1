$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot

Set-Location $ProjectRoot

Write-Host "=== BirdApp setup ===" -ForegroundColor Cyan

Write-Host ""
Write-Host "Provjeravam Docker..." -ForegroundColor Yellow

docker info | Out-Null

if ($LASTEXITCODE -ne 0) {
    throw "Docker nije pokrenut. Pokreni Docker Desktop i pokušaj ponovno."
}

Write-Host "Docker je dostupan." -ForegroundColor Green

Write-Host ""
Write-Host "Pokrećem MongoDB, MinIO i Kafka..." -ForegroundColor Yellow

docker compose up -d

if ($LASTEXITCODE -ne 0) {
    throw "docker compose up -d nije uspio."
}

function Wait-ForPort {
    param(
        [string]$HostName,
        [int]$Port,
        [string]$ServiceName
    )

    Write-Host "Čekam $ServiceName na portu $Port..." -ForegroundColor Yellow

    for ($i = 0; $i -lt 60; $i++) {
        try {
            $client = New-Object System.Net.Sockets.TcpClient
            $client.Connect($HostName, $Port)
            $client.Close()

            Write-Host "$ServiceName je spreman." -ForegroundColor Green
            return
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    throw "$ServiceName nije postao dostupan."
}

Wait-ForPort "localhost" 27017 "MongoDB"
Wait-ForPort "localhost" 9000 "MinIO"
Wait-ForPort "localhost" 9092 "Kafka"

Write-Host ""
Write-Host "Provjeravam Kafka topic..." -ForegroundColor Yellow

$topics = docker exec birdapp-kafka /opt/kafka/bin/kafka-topics.sh `
    --bootstrap-server localhost:9092 `
    --list

if ($topics -notcontains "bird-observations") {
    Write-Host "Kreiram Kafka topic: bird-observations" -ForegroundColor Yellow

    docker exec birdapp-kafka /opt/kafka/bin/kafka-topics.sh `
        --bootstrap-server localhost:9092 `
        --create `
        --topic bird-observations `
        --partitions 1 `
        --replication-factor 1

    if ($LASTEXITCODE -ne 0) {
        throw "Kreiranje Kafka topica nije uspjelo."
    }

    Write-Host "Kafka topic kreiran." -ForegroundColor Green
}
else {
    Write-Host "Kafka topic već postoji." -ForegroundColor Green
}

Write-Host ""
Write-Host "Pokrećem BirdApp..." -ForegroundColor Cyan
Write-Host ""

dotnet run --project "$ProjectRoot\BirdApp.csproj"

if ($LASTEXITCODE -ne 0) {
    throw "BirdApp nije uspješno završio."
}

Write-Host ""
Write-Host "=== BirdApp završen ===" -ForegroundColor Green