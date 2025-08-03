#!/bin/bash

# Club Management Platform - Database Migration Script
# This script runs EF Core migrations and database seeding

set -e  # Exit on any error

echo "🗄️ Club Management Platform - Database Migration"
echo "=============================================="

# Show usage if help requested
if [[ "$1" == "--help" ]] || [[ "$1" == "-h" ]]; then
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  migrate             Apply pending migrations to database (default)"
    echo "  seed                Run database seeding (requires migrations to be applied first)"
    echo "  reset               Drop database and recreate with migrations and seed data (⚠️  DESTRUCTIVE)"
    echo "  status              Show migration status"
    echo "  add <name>          Create a new migration with the given name"
    echo "  remove              Remove the last migration (only if not applied to database)"
    echo "  -h, --help          Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                  # Apply pending migrations"
    echo "  $0 migrate          # Apply pending migrations"  
    echo "  $0 seed             # Run database seeding"
    echo "  $0 reset            # Drop and recreate database (⚠️  WARNING: DELETES ALL DATA)"
    echo "  $0 status           # Show current migration status"
    echo "  $0 add AddNewTable  # Create new migration named 'AddNewTable'"
    echo ""
    exit 0
fi

# Check if .NET 9 is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET 9 is not installed. Please install .NET 9 SDK first."
    exit 1
fi

# Check if docker/.env exists
if [ ! -f "docker/.env" ]; then
    echo "❌ docker/.env file not found. Please run './scripts/setup.sh' first."
    exit 1
fi

# Load environment variables
echo "📖 Loading environment variables..."
set -a
source docker/.env
set +a

# Navigate to API directory
API_DIR="src/Api/ClubManagement.Api"
INFRASTRUCTURE_PROJECT="../../Infrastructure/ClubManagement.Infrastructure"

if [ ! -d "$API_DIR" ]; then
    echo "❌ API directory not found: $API_DIR"
    exit 1
fi

cd "$API_DIR"

# Parse command
command=${1:-migrate}

case $command in
    "migrate"|"")
        echo "🔄 Applying database migrations..."
        dotnet ef database update --project "$INFRASTRUCTURE_PROJECT" --startup-project . --context ClubManagementDbContext
        echo "✅ Database migrations applied successfully!"
        ;;
    "seed")
        echo "🌱 Running database seeding..."
        echo "💡 Note: Seeding is automatically performed when the API starts"
        echo "   To manually trigger seeding, start the API: dotnet run"
        ;;
    "reset")
        echo "⚠️  WARNING: This will DELETE ALL DATA in the database!"
        echo "Are you sure you want to continue? (yes/no)"
        read -r response
        if [[ "$response" == "yes" ]]; then
            echo "🗑️  Dropping database..."
            dotnet ef database drop --force --project "$INFRASTRUCTURE_PROJECT" --startup-project . --context ClubManagementDbContext
            echo "🔄 Applying migrations to recreate database..."
            dotnet ef database update --project "$INFRASTRUCTURE_PROJECT" --startup-project . --context ClubManagementDbContext
            echo "✅ Database reset completed!"
            echo "💡 Start the API to run automatic seeding: dotnet run"
        else
            echo "❌ Database reset cancelled"
        fi
        ;;
    "status")
        echo "📊 Current migration status:"
        dotnet ef migrations list --project "$INFRASTRUCTURE_PROJECT" --startup-project . --context ClubManagementDbContext
        ;;
    "add")
        if [ -z "$2" ]; then
            echo "❌ Migration name is required"
            echo "Usage: $0 add <MigrationName>"
            echo "Example: $0 add AddUserPreferences"
            exit 1
        fi
        echo "➕ Creating new migration: $2"
        dotnet ef migrations add "$2" --project "$INFRASTRUCTURE_PROJECT" --startup-project . --context ClubManagementDbContext
        echo "✅ Migration '$2' created successfully!"
        echo "💡 Run '$0 migrate' to apply it to the database"
        ;;
    "remove")
        echo "➖ Removing last migration..."
        dotnet ef migrations remove --project "$INFRASTRUCTURE_PROJECT" --startup-project . --context ClubManagementDbContext
        echo "✅ Last migration removed successfully!"
        ;;
    *)
        echo "❌ Unknown command: $command"
        echo "Run '$0 --help' for usage information"
        exit 1
        ;;
esac

echo ""
echo "🎉 Operation completed successfully!"
echo ""
echo "📋 Next steps:"
echo "   • To apply migrations: $0 migrate"
echo "   • To start API (includes seeding): cd $API_DIR && dotnet run"
echo "   • To view migration status: $0 status"