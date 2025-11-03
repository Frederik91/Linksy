#!/bin/bash
# Add a new EF Core migration

if [ -z "$1" ]; then
    echo "Usage: ./add-migration.sh <MigrationName>"
    echo "Example: ./add-migration.sh AddUserTable"
    exit 1
fi

MIGRATION_NAME=$1

echo "Adding migration: $MIGRATION_NAME"
cd "$(dirname "$0")/../src/Linksy.Api" || exit

dotnet ef migrations add "$MIGRATION_NAME" \
    --context ApplicationDbContext \
    --output-dir Data/Migrations

echo "Migration $MIGRATION_NAME added successfully!"
echo "The migration will be automatically applied when you run the application."
