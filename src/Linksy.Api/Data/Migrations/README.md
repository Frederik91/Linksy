# Database Migrations

This directory contains EF Core migrations for the Linksy database schema.

## Creating a New Migration

To create a new migration after modifying entity models:

```bash
# From repository root
./scripts/add-migration.sh YourMigrationName

# Or manually from src/Linksy.Api:
dotnet ef migrations add YourMigrationName \
    --context ApplicationDbContext \
    --output-dir Data/Migrations
```

## Applying Migrations

Migrations are **automatically applied** when the application starts via the `Linksy.Migrations` service. No manual steps required!

## Initial Setup

When you first clone the repository and run:

```bash
cd src
dotnet run --project Linksy.AppHost
```

The following happens automatically:
1. PostgreSQL container starts
2. Migrations service creates the database
3. All pending migrations are applied
4. Application starts with a fresh schema

## Migration Files

Each migration consists of three files:
- `{timestamp}_{MigrationName}.cs` - The migration operations
- `{timestamp}_{MigrationName}.Designer.cs` - EF Core metadata
- `ApplicationDbContextModelSnapshot.cs` - Current schema snapshot

## Troubleshooting

### No migrations exist yet
If you see this message, generate the initial migration:
```bash
./scripts/add-migration.sh InitialCreate
```

### Migration pending but not applied
The migrations service will automatically detect and apply pending migrations on next startup.

### Reset database to clean state
```bash
# Stop the application
# Remove the Docker volume
docker volume rm linksy-postgres-data

# Restart - migrations will recreate everything
cd src
dotnet run --project Linksy.AppHost
```
