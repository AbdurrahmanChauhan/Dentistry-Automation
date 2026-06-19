@description Target API URL for the web console
param(
    [string]$ApiUrl = "http://localhost:5000"
)

Write-Host "Dentistry Automation Platform - Local Setup" -ForegroundColor Cyan

# Start infrastructure
Write-Host "`nStarting Docker infrastructure..." -ForegroundColor Yellow
docker compose up -d sqlserver redis azurite

Write-Host "Waiting for SQL Server..."
Start-Sleep -Seconds 20

# Python AI worker
Write-Host "`nStarting AI worker..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot\src\ai-workers'; pip install -r requirements.txt; python -m uvicorn workers.main:app --host 0.0.0.0 --port 8000"

# Note: .NET API requires Docker or .NET 8 SDK
Write-Host "`nTo run the API:" -ForegroundColor Green
Write-Host "  docker compose up api"
Write-Host "  OR: dotnet run --project src/platform-api"

Write-Host "`nTo run the web UI:" -ForegroundColor Green
Write-Host "  cd src/platform-web && npm install && npm run dev"

Write-Host "`nDemo API Key: da-demo-key-change-in-production" -ForegroundColor Cyan
