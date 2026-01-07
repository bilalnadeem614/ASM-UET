# ASM-UET Production Deployment Script
# This script publishes the application with Production environment settings

Write-Host "?? Starting ASM-UET Production Deployment..." -ForegroundColor Green

# Navigate to project directory
Set-Location "ASM-UET"

# Clean previous builds
Write-Host "?? Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean --configuration Release

# Restore packages
Write-Host "?? Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore

# Build in Release mode
Write-Host "?? Building application in Release mode..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Build failed! Please fix errors before publishing." -ForegroundColor Red
    exit 1
}

# Publish application
Write-Host "?? Publishing application for Production..." -ForegroundColor Yellow
dotnet publish --configuration Release --output "bin\Release\net8.0\publish" --no-build

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Application published successfully!" -ForegroundColor Green
    Write-Host "?? Published files location: $(Get-Location)\bin\Release\net8.0\publish" -ForegroundColor Cyan
    Write-Host "?? Environment: Production (set via web.config)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "?? Next steps:" -ForegroundColor White
    Write-Host "1. Navigate to: $(Get-Location)\bin\Release\net8.0\publish" -ForegroundColor Gray
    Write-Host "2. Zip all contents of the publish folder" -ForegroundColor Gray
    Write-Host "3. Upload the zip file to your hosting provider" -ForegroundColor Gray
    Write-Host "4. Extract on the server" -ForegroundColor Gray
} else {
    Write-Host "? Publish failed!" -ForegroundColor Red
    exit 1
}

# Navigate back
Set-Location ".."

Write-Host "?? Deployment preparation complete!" -ForegroundColor Green