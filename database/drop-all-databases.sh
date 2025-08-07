#!/bin/bash

# Drop all ClubManagement databases
# Safely drops catalog database and all tenant databases with confirmation

# Source database configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/db-config.sh"

# Function to terminate connections to a database
terminate_connections() {
    local database="$1"
    echo -e "${YELLOW}🔌 Terminating connections to $database...${NC}"
    execute_sql "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$database';" postgres > /dev/null
}

# Function to drop database safely
drop_database_safe() {
    local database="$1"
    
    # Check if database exists
    if ! execute_sql "SELECT 1 FROM pg_database WHERE datname = '$database';" postgres | grep -q "1"; then
        echo -e "${YELLOW}⚠️  Database '$database' does not exist${NC}"
        return 0
    fi
    
    echo -e "${BLUE}🗑️  Dropping database: $database${NC}"
    
    # Terminate connections
    terminate_connections "$database"
    
    # Drop database
    if execute_sql "DROP DATABASE IF EXISTS \"$database\";" postgres > /dev/null; then
        echo -e "${GREEN}✅ Successfully dropped $database${NC}"
    else
        echo -e "${RED}❌ Failed to drop $database${NC}"
        return 1
    fi
}

echo -e "${RED}🗑️  ClubManagement Database Cleanup${NC}"
echo "===================================="
echo ""

# Test connection first
if ! test_connection; then
    echo -e "${RED}❌ Cannot connect to database server${NC}"
    exit 1
fi

# Show current databases
echo -e "${BLUE}📋 Current ClubManagement databases:${NC}"
execute_sql "SELECT datname FROM pg_database WHERE datname LIKE 'clubmanagement%' ORDER BY datname;" postgres

echo ""

# Confirmation prompt unless --force flag is used
if [[ "$1" != "--force" ]] && [[ "$1" != "-f" ]]; then
    echo -e "${YELLOW}⚠️  WARNING: This will drop ALL ClubManagement databases!${NC}"
    echo -e "${YELLOW}   This includes:${NC}"
    echo -e "${YELLOW}   • Catalog database (tenant registry)${NC}"
    echo -e "${YELLOW}   • All tenant databases (user data)${NC}"
    echo -e "${YELLOW}   • This action CANNOT be undone!${NC}"
    echo ""
    read -p "Are you sure you want to continue? Type 'YES' to confirm: " confirmation
    
    if [[ "$confirmation" != "YES" ]]; then
        echo -e "${BLUE}ℹ️  Operation cancelled${NC}"
        exit 0
    fi
fi

echo ""
echo -e "${RED}🚀 Starting database cleanup...${NC}"

# Get all ClubManagement databases
databases=$(execute_sql "SELECT datname FROM pg_database WHERE datname LIKE 'clubmanagement%' ORDER BY datname;" postgres | grep clubmanagement | tr -d ' ')

if [[ -z "$databases" ]]; then
    echo -e "${GREEN}✅ No ClubManagement databases found${NC}"
    exit 0
fi

# Drop each database
for database in $databases; do
    drop_database_safe "$database"
done

echo ""
echo -e "${GREEN}🎉 Database cleanup completed!${NC}"

# Verify cleanup
remaining=$(execute_sql "SELECT count(*) FROM pg_database WHERE datname LIKE 'clubmanagement%';" postgres | grep -oE '[0-9]+' | head -1)

if [[ "$remaining" -eq 0 ]]; then
    echo -e "${GREEN}✅ All ClubManagement databases have been removed${NC}"
else
    echo -e "${YELLOW}⚠️  $remaining databases still remain - please check manually${NC}"
fi

echo ""
echo -e "${BLUE}💡 Next steps:${NC}"
echo "• Run the API to automatically recreate databases with fresh data"
echo "• Or use Entity Framework migrations: 'dotnet ef database update'"