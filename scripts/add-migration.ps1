# Add a new EF Core migration

param(
    [Parameter(Mandatory=$true)]
    [string]$MigrationName
)

Write-Host "Adding migration: $MigrationName" -ForegroundColor Green

Push-Location "$PSScriptRoot\..\src\Linksy.Api"

dotnet ef migrations add $MigrationName `
    --context ApplicationDbContext `
    --output-dir Data/Migrations

Pop-Location

Write-Host "Migration $MigrationName added successfully!" -ForegroundColor Green
Write-Host "The migration will be automatically applied when you run the application." -ForegroundColor Cyan
